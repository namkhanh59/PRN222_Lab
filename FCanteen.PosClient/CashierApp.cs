using FCanteen.PosClient.Models;
using FCanteen.PosClient.Networking;
using FCanteen.PosClient.Services;
using Microsoft.Extensions.Configuration;

// Đọc tên quầy từ command-line argument: dotnet run -- QUAY01
string counterName = args.Length > 0 ? args[0] : "QUAY_UNKNOWN";

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Không tìm thấy connection string.");

var menuService = new MenuService(connectionString);
var tcpClient = new TcpOrderClient("127.0.0.1", 9500);

// Chạy UDP listener nền để nhận thông báo hết món bất cứ lúc nào
var udpListener = new UdpNotificationListener();
_ = Task.Run(() => udpListener.StartListeningAsync());

// Service đồng bộ giá qua HttpClient
var priceSyncService = new PriceSyncService(connectionString);
var priceSyncUrl = config["PriceSync:SourceUrl"] ?? "";

Console.WriteLine($"=== FCANTEEN POS CLIENT - {counterName} ===");

while (true)
{
    var menu = await menuService.GetAvailableMenuAsync();

    // Lọc thêm những món vừa được UDP báo hết hàng (real-time, chưa kịp cập nhật lại từ DB)
    var displayMenu = menu.Where(m => !udpListener.OutOfStockItemIds.Contains(m.Id)).ToList();

    Console.WriteLine("\n---- THỰC ĐƠN ----");
    for (int i = 0; i < displayMenu.Count; i++)
    {
        Console.WriteLine($"{i + 1}. {displayMenu[i].Name,-25} {displayMenu[i].Price,10:N0}đ / {displayMenu[i].Unit}");
    }
    Console.WriteLine("0. Thoát chương trình");

    var orderLines = new List<OrderLineDto>();
    decimal tempTotal = 0;

    Console.WriteLine("\nNhập từng dòng món (số thứ tự = 0 để dừng nhập và gửi order, gõ 'sync' để đồng bộ giá):");

    while (true)
    {
        Console.Write("Số thứ tự món: ");
        var input = Console.ReadLine();

        // Lệnh đặc biệt: đồng bộ giá qua HTTP
        if (input?.Trim().Equals("sync", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (string.IsNullOrWhiteSpace(priceSyncUrl))
            {
                Console.WriteLine("Chưa cấu hình PriceSync:SourceUrl trong appsettings.json.");
            }
            else
            {
                await priceSyncService.SyncPricesAsync(priceSyncUrl);
            }
            continue;
        }

        if (!int.TryParse(input, out int choice))
        {
            Console.WriteLine("Vui lòng nhập số hợp lệ.");
            continue;
        }

        if (choice == 0) break;

        if (choice < 1 || choice > displayMenu.Count)
        {
            Console.WriteLine("Số thứ tự không hợp lệ.");
            continue;
        }

        var selectedItem = displayMenu[choice - 1];

        Console.Write("Số lượng: ");
        if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
        {
            Console.WriteLine("Số lượng không hợp lệ.");
            continue;
        }

        Console.Write("Ghi chú (Enter để bỏ qua): ");
        var note = Console.ReadLine();

        orderLines.Add(new OrderLineDto
        {
            MenuItemId = selectedItem.Id,
            Quantity = qty,
            Note = string.IsNullOrWhiteSpace(note) ? null : note
        });

        var lineTotal = selectedItem.Price * qty;
        tempTotal += lineTotal;

        Console.WriteLine($"  → Đã thêm: {selectedItem.Name} x{qty} = {lineTotal:N0}đ");
        Console.WriteLine($"  Tạm tính hiện tại: {tempTotal:N0}đ");
    }

    if (orderLines.Count == 0)
    {
        Console.Write("\nĐơn hàng trống. Thoát chương trình? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() == "y") break;
        continue;
    }

    Console.WriteLine($"\nĐang gửi phiếu order lên bếp... (Tạm tính: {tempTotal:N0}đ)");

    try
    {
        var request = new OrderRequestDto { CounterName = counterName, Lines = orderLines };
        var response = await tcpClient.SendOrderAsync(request);

        if (response.Success)
        {
            Console.WriteLine($"\n✅ THÀNH CÔNG! Mã phiếu: {response.TicketCode} - Tổng tiền (server tính): {response.TotalAmount:N0}đ");
        }
        else
        {
            Console.WriteLine($"\n❌ THẤT BẠI: {response.ErrorMessage}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ Lỗi kết nối tới server: {ex.Message}");
    }

    Console.Write("\nTạo phiếu mới? (y/n): ");
    if (Console.ReadLine()?.Trim().ToLower() != "y") break;
}

Console.WriteLine("Đã đóng quầy. Tạm biệt!");
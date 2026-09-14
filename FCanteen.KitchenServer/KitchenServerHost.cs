using FCanteen.KitchenServer.Networking;
using FCanteen.KitchenServer.Services;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Không tìm thấy connection string.");

var orderService = new OrderService(connectionString);
var udpBroadcaster = new UdpBroadcaster();
var server = new TcpOrderServer(9500, orderService, udpBroadcaster);

Console.WriteLine("=== FCANTEEN KITCHEN SERVER ===");

// Chạy TCP server ở 1 Task nền
_ = Task.Run(() => server.StartAsync());

// Vòng lặp đọc lệnh console để mô phỏng thao tác của bếp
Console.WriteLine("Gõ 'outofstock <id>' để đánh dấu 1 món hết hàng, hoặc 'menu' để xem danh sách món, 'exit' để thoát.");

while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;

    var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    if (parts[0].Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    else if (parts[0].Equals("menu", StringComparison.OrdinalIgnoreCase))
    {
        var items = await orderService.GetAllMenuItemsAsync();
        foreach (var m in items)
        {
            Console.WriteLine($"  [{m.Id}] {m.Name} - {m.Price:N0}đ - {(m.IsAvailable ? "Còn bán" : "HẾT HÀNG")}");
        }
    }
    else if (parts[0].Equals("outofstock", StringComparison.OrdinalIgnoreCase) && parts.Length > 1)
    {
        if (int.TryParse(parts[1], out int id))
        {
            var item = await orderService.MarkOutOfStockAsync(id);
            if (item == null)
            {
                Console.WriteLine("Không tìm thấy món có id này.");
            }
            else
            {
                Console.WriteLine($"Đã đánh dấu hết hàng: {item.Name}");
                await udpBroadcaster.BroadcastOutOfStockAsync(item.Id, item.Name);
            }
        }
        else
        {
            Console.WriteLine("Cú pháp: outofstock <id>");
        }
    }
    else
    {
        Console.WriteLine("Lệnh không hợp lệ. Dùng: menu | outofstock <id> | exit");
    }
}
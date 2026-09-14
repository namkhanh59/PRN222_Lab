using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FCanteen.KitchenServer.Models;
using FCanteen.KitchenServer.Services;

namespace FCanteen.KitchenServer.Networking;

public class TcpOrderServer
{
    private readonly int _port;
    private readonly OrderService _orderService;
    private readonly UdpBroadcaster _udpBroadcaster;
    private TcpListener? _listener;

    public TcpOrderServer(int port, OrderService orderService, UdpBroadcaster udpBroadcaster)
    {
        _port = port;
        _orderService = orderService;
        _udpBroadcaster = udpBroadcaster;
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        Console.WriteLine($"[KitchenServer] Đang lắng nghe TCP tại cổng {_port}...");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            // Mỗi kết nối xử lý trên 1 Task riêng, không block vòng lặp accept
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var remoteEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[TCP] Quầy kết nối vào: {remoteEndpoint}");
        await _orderService.LogDeviceAsync("TCP", remoteEndpoint, "Kết nối vào");

        try
        {
            using var stream = client.GetStream();

            // Đọc toàn bộ dữ liệu client gửi lên (JSON kết thúc bằng ký tự xuống dòng)
            var json = await ReadMessageAsync(stream);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"[TCP] {remoteEndpoint} gửi dữ liệu rỗng, đóng kết nối.");
                return;
            }

            Console.WriteLine($"[TCP] Nhận từ {remoteEndpoint}: {json}");

            var request = JsonSerializer.Deserialize<OrderRequestDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            OrderResponseDto response;
            if (request == null)
            {
                response = new OrderResponseDto { Success = false, ErrorMessage = "Dữ liệu JSON không hợp lệ." };
            }
            else
            {
                response = await _orderService.ProcessOrderAsync(request);

                if (response.Success)
                {
                    Console.WriteLine($"[ORDER] Đã lưu phiếu {response.TicketCode} - Quầy {request.CounterName} - Tổng: {response.TotalAmount:N0}đ");
                    await PrintPendingTicketsAsync();
                }
            }

            var responseJson = JsonSerializer.Serialize(response);
            await SendMessageAsync(stream, responseJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TCP] Lỗi xử lý kết nối {remoteEndpoint}: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"[TCP] Quầy ngắt kết nối: {remoteEndpoint}");
            await _orderService.LogDeviceAsync("TCP", remoteEndpoint, "Ngắt kết nối");
            client.Close();
        }
    }

    private async Task PrintPendingTicketsAsync()
    {
        var tickets = await _orderService.GetPendingTicketsAsync();
        Console.WriteLine("---- Danh sách phiếu đang chờ chế biến ----");
        foreach (var t in tickets)
        {
            Console.WriteLine($"  {t.TicketCode} | Quầy: {t.CounterName} | Tổng: {t.TotalAmount:N0}đ | {t.CreatedAt:HH:mm:ss}");
            foreach (var line in t.TicketLines)
            {
                Console.WriteLine($"      - {line.MenuItem.Name} x{line.Quantity} ({line.Note})");
            }
        }
        Console.WriteLine("-------------------------------------------");
    }

    // Đọc message: dùng độ dài dòng (đọc tới khi gặp '\n') làm ranh giới message
    private static async Task<string> ReadMessageAsync(NetworkStream stream)
    {
        var buffer = new List<byte>();
        var oneByte = new byte[1];

        while (true)
        {
            int bytesRead = await stream.ReadAsync(oneByte, 0, 1);
            if (bytesRead == 0) break; // client đóng kết nối
            if (oneByte[0] == (byte)'\n') break;
            buffer.Add(oneByte[0]);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static async Task SendMessageAsync(NetworkStream stream, string message)
    {
        var data = Encoding.UTF8.GetBytes(message + "\n");
        await stream.WriteAsync(data, 0, data.Length);
    }
}
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FCanteen.PosClient.Models;

namespace FCanteen.PosClient.Networking;

public class TcpOrderClient
{
    private readonly string _serverHost;
    private readonly int _serverPort;

    public TcpOrderClient(string serverHost, int serverPort)
    {
        _serverHost = serverHost;
        _serverPort = serverPort;
    }

    public async Task<OrderResponseDto> SendOrderAsync(OrderRequestDto request)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_serverHost, _serverPort);

        using var stream = client.GetStream();

        var json = JsonSerializer.Serialize(request);
        await SendMessageAsync(stream, json);

        var responseJson = await ReadMessageAsync(stream);

        var response = JsonSerializer.Deserialize<OrderResponseDto>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return response ?? new OrderResponseDto { Success = false, ErrorMessage = "Không nhận được phản hồi hợp lệ từ server." };
    }

    // Phải khớp quy ước delimiter '\n' với server
    private static async Task SendMessageAsync(NetworkStream stream, string message)
    {
        var data = Encoding.UTF8.GetBytes(message + "\n");
        await stream.WriteAsync(data, 0, data.Length);
    }

    private static async Task<string> ReadMessageAsync(NetworkStream stream)
    {
        var buffer = new List<byte>();
        var oneByte = new byte[1];

        while (true)
        {
            int bytesRead = await stream.ReadAsync(oneByte, 0, 1);
            if (bytesRead == 0) break;
            if (oneByte[0] == (byte)'\n') break;
            buffer.Add(oneByte[0]);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
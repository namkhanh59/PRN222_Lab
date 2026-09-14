using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FCanteen.PosClient.Networking;

public class UdpNotificationListener
{
    private const int UdpPort = 9600;

    // Set chứa các menuItemId đã bị đánh dấu hết hàng (nhận qua UDP)
    public HashSet<int> OutOfStockItemIds { get; } = new();

    public async Task StartListeningAsync()
    {
        using var udpClient = new UdpClient();
        udpClient.ExclusiveAddressUse = false;
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, UdpPort));

        while (true)
        {
            try
            {
                var result = await udpClient.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);

                // Định dạng: OUT_OF_STOCK|<id>|<tên món>
                var parts = message.Split('|');
                if (parts.Length == 3 && parts[0] == "OUT_OF_STOCK" && int.TryParse(parts[1], out int itemId))
                {
                    OutOfStockItemIds.Add(itemId);
                    Console.WriteLine($"\n🔔 [THÔNG BÁO] Món '{parts[2]}' (id={itemId}) đã HẾT HÀNG. Không thể chọn món này nữa.\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UDP Listener] Lỗi: {ex.Message}");
            }
        }
    }
}
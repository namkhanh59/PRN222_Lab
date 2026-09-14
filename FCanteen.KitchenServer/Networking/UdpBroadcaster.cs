using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FCanteen.KitchenServer.Networking;

public class UdpBroadcaster
{
    private const int UdpPort = 9600;

    // Sẽ hoàn thiện đầy đủ ở YC4 — hiện tại để server build được
    public async Task BroadcastOutOfStockAsync(int menuItemId, string menuItemName)
    {
        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        var message = $"OUT_OF_STOCK|{menuItemId}|{menuItemName}";
        var data = Encoding.UTF8.GetBytes(message);

        await udpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, UdpPort));
        Console.WriteLine($"[UDP] Đã phát thông báo hết món: {menuItemName}");
    }
}
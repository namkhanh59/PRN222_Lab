using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FCanteen.Data;
using FCanteen.PosClient.Models;
using Microsoft.EntityFrameworkCore;

namespace FCanteen.PosClient.Networking;

public class PriceSyncService
{
    private readonly string _connectionString;
    private readonly HttpClient _httpClient;

    public PriceSyncService(string connectionString)
    {
        _connectionString = connectionString;
        _httpClient = new HttpClient();
    }

    public async Task SyncPricesAsync(string sourceUrl)
    {
        var uri = new Uri(sourceUrl);
        Console.WriteLine("\n---- PHÂN TÍCH ĐỊA CHỈ NGUỒN ----");
        Console.WriteLine($"Scheme : {uri.Scheme}");
        Console.WriteLine($"Host   : {uri.Host}");
        Console.WriteLine($"Port   : {uri.Port}");

        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(uri.Host);
            Console.WriteLine("Địa chỉ IP phân giải được:");
            foreach (var ip in hostEntry.AddressList)
            {
                Console.WriteLine($"  - {ip}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Không thể phân giải DNS: {ex.Message}");
        }
        Console.WriteLine("----------------------------------\n");

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;
        string logContent;
        int statusCode;

        try
        {
            response = await _httpClient.GetAsync(uri);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                logContent = $"GET {uri} thất bại, status={statusCode}, thời gian={stopwatch.ElapsedMilliseconds}ms";
                Console.WriteLine(logContent);
                await LogHttpCallAsync(uri.ToString(), logContent);
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<PriceSyncItemDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<PriceSyncItemDto>();

            int updatedCount = await ApplyPriceUpdatesAsync(items);

            logContent = $"GET {uri} thành công, status={statusCode}, thời gian={stopwatch.ElapsedMilliseconds}ms, cập nhật {updatedCount} món";
            Console.WriteLine(logContent);
            await LogHttpCallAsync(uri.ToString(), logContent);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logContent = $"GET {uri} lỗi: {ex.Message}, thời gian={stopwatch.ElapsedMilliseconds}ms";
            Console.WriteLine(logContent);
            await LogHttpCallAsync(uri.ToString(), logContent);
        }
    }

    private async Task<int> ApplyPriceUpdatesAsync(List<PriceSyncItemDto> items)
    {
        var options = new DbContextOptionsBuilder<FCanteenContext>()
            .UseSqlServer(_connectionString)
            .Options;

        using var context = new FCanteenContext(options);
        int updatedCount = 0;

        foreach (var incoming in items)
        {
            var dbItem = await context.MenuItems.FirstOrDefaultAsync(m => m.Code == incoming.Code);
            if (dbItem == null) continue;

            if (dbItem.Price != incoming.Price)
            {
                Console.WriteLine($"  Cập nhật giá: {dbItem.Name} {dbItem.Price:N0}đ -> {incoming.Price:N0}đ");
                dbItem.Price = incoming.Price;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync();
        }

        return updatedCount;
    }

    private async Task LogHttpCallAsync(string sourceAddress, string content)
    {
        var options = new DbContextOptionsBuilder<FCanteenContext>()
            .UseSqlServer(_connectionString)
            .Options;

        using var context = new FCanteenContext(options);
        context.DeviceLogs.Add(new Data.Entities.DeviceLog
        {
            Protocol = "HTTP",
            SourceAddress = sourceAddress,
            Content = content,
            Timestamp = DateTime.Now
        });
        await context.SaveChangesAsync();
    }
}
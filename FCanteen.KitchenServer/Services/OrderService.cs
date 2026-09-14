using FCanteen.Data;
using FCanteen.Data.Entities;
using FCanteen.KitchenServer.Models;
using Microsoft.EntityFrameworkCore;

namespace FCanteen.KitchenServer.Services;

public class OrderService
{
    private readonly string _connectionString;

    public OrderService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private FCanteenContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FCanteenContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new FCanteenContext(options);
    }

    // Xử lý 1 order: validate món, tính lại giá, lưu OrderTicket + TicketLine trong 1 transaction
    public async Task<OrderResponseDto> ProcessOrderAsync(OrderRequestDto request)
    {
        using var context = CreateContext();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            if (request.Lines == null || request.Lines.Count == 0)
            {
                return new OrderResponseDto { Success = false, ErrorMessage = "Đơn hàng trống." };
            }

            var menuItemIds = request.Lines.Select(l => l.MenuItemId).Distinct().ToList();
            var menuItems = await context.MenuItems
                .Where(m => menuItemIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            // Validate: món có tồn tại và còn bán không
            foreach (var line in request.Lines)
            {
                if (!menuItems.TryGetValue(line.MenuItemId, out var item))
                    return new OrderResponseDto { Success = false, ErrorMessage = $"Món id={line.MenuItemId} không tồn tại." };

                if (!item.IsAvailable)
                    return new OrderResponseDto { Success = false, ErrorMessage = $"Món '{item.Name}' đã hết hàng." };

                if (line.Quantity <= 0)
                    return new OrderResponseDto { Success = false, ErrorMessage = "Số lượng không hợp lệ." };
            }

            var ticket = new OrderTicket
            {
                TicketCode = GenerateTicketCode(),
                CounterName = request.CounterName,
                CreatedAt = DateTime.Now,
                Status = TicketStatus.Pending
            };

            decimal total = 0;
            var ticketLines = new List<TicketLine>();

            // QUAN TRỌNG: lấy giá từ DB tại thời điểm này, KHÔNG tin giá do client gửi lên
            foreach (var line in request.Lines)
            {
                var item = menuItems[line.MenuItemId];
                var lineTotal = item.Price * line.Quantity;
                total += lineTotal;

                ticketLines.Add(new TicketLine
                {
                    MenuItemId = item.Id,
                    Quantity = line.Quantity,
                    UnitPrice = item.Price,   // chốt giá tại thời điểm bán
                    Note = line.Note
                });
            }

            ticket.TotalAmount = total;
            ticket.TicketLines = ticketLines;

            context.OrderTickets.Add(ticket);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new OrderResponseDto
            {
                Success = true,
                TicketCode = ticket.TicketCode,
                TotalAmount = ticket.TotalAmount
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new OrderResponseDto { Success = false, ErrorMessage = "Lỗi hệ thống: " + ex.Message };
        }
    }

    public async Task LogDeviceAsync(string protocol, string sourceAddress, string content)
    {
        using var context = CreateContext();
        context.DeviceLogs.Add(new DeviceLog
        {
            Protocol = protocol,
            SourceAddress = sourceAddress,
            Content = content,
            Timestamp = DateTime.Now
        });
        await context.SaveChangesAsync();
    }

    public async Task<List<OrderTicket>> GetPendingTicketsAsync()
    {
        using var context = CreateContext();
        return await context.OrderTickets
            .Include(t => t.TicketLines)
            .ThenInclude(l => l.MenuItem)
            .Where(t => t.Status == TicketStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
    }

    private string GenerateTicketCode()
    {
        return "TK" + DateTime.Now.ToString("yyMMddHHmmssfff");
    }

    public async Task<Data.Entities.MenuItem?> MarkOutOfStockAsync(int menuItemId)
   {
    using var context = CreateContext();
    var item = await context.MenuItems.FindAsync(menuItemId);
    if (item == null) return null;

    item.IsAvailable = false;
    await context.SaveChangesAsync();
    return item;
   }

public async Task<List<Data.Entities.MenuItem>> GetAllMenuItemsAsync()
{
    using var context = CreateContext();
    return await context.MenuItems.OrderBy(m => m.Id).ToListAsync();
}
}
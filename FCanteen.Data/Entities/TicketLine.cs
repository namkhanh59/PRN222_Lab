namespace FCanteen.Data.Entities;

public class TicketLine
{
    public int Id { get; set; }

    public int OrderTicketId { get; set; }
    public OrderTicket OrderTicket { get; set; } = null!;

    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public int Quantity { get; set; }

    // Quan trọng: lưu giá tại thời điểm bán, KHÔNG đọc lại từ MenuItem sau này
    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }
}

namespace FCanteen.Data.Entities;

public enum TicketStatus
{
    Pending,
    Preparing,
    Completed,
    Cancelled
}

public class OrderTicket
{
    public int Id { get; set; }

    public string TicketCode { get; set; } = string.Empty; // mã phiếu

    public string CounterName { get; set; } = string.Empty; // tên quầy gửi

    public decimal TotalAmount { get; set; }                // tổng tiền

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public TicketStatus Status { get; set; } = TicketStatus.Pending;

    public ICollection<TicketLine> TicketLines { get; set; } = new List<TicketLine>();
}
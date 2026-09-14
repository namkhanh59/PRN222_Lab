namespace FCanteen.PosClient.Models;

public class OrderRequestDto
{
    public string CounterName { get; set; } = string.Empty;
    public List<OrderLineDto> Lines { get; set; } = new();
}

public class OrderLineDto
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}

public class OrderResponseDto
{
    public bool Success { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? ErrorMessage { get; set; }
}
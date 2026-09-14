namespace FCanteen.KitchenServer.Models;

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

// Server trả về cho client sau khi lưu xong
public class OrderResponseDto
{
    public bool Success { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? ErrorMessage { get; set; }
}
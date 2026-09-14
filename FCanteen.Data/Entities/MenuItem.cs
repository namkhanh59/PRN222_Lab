using System.ComponentModel.DataAnnotations;

namespace FCanteen.Data.Entities;

public class MenuItem
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;   // mã món

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;   // tên món

    public decimal Price { get; set; }                 // giá bán

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;    // đơn vị tính (phần, ly, ...)

    public bool IsAvailable { get; set; } = true;       // còn bán hay không

    public ICollection<TicketLine> TicketLines { get; set; } = new List<TicketLine>();
}
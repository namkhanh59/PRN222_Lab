using Microsoft.EntityFrameworkCore;
using FCanteen.Data.Entities;

namespace FCanteen.Data;

public class FCanteenContext : DbContext
{
    public FCanteenContext(DbContextOptions<FCanteenContext> options) : base(options) { }

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<OrderTicket> OrderTickets => Set<OrderTicket>();
    public DbSet<TicketLine> TicketLines => Set<TicketLine>();
    public DbSet<DeviceLog> DeviceLogs => Set<DeviceLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Quan hệ TicketLine -> OrderTicket
        modelBuilder.Entity<TicketLine>()
            .HasOne(tl => tl.OrderTicket)
            .WithMany(t => t.TicketLines)
            .HasForeignKey(tl => tl.OrderTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quan hệ TicketLine -> MenuItem
        modelBuilder.Entity<TicketLine>()
            .HasOne(tl => tl.MenuItem)
            .WithMany(m => m.TicketLines)
            .HasForeignKey(tl => tl.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict); // không cho xoá món nếu đã có trong hoá đơn

        modelBuilder.Entity<MenuItem>()
            .Property(m => m.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<TicketLine>()
            .Property(t => t.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrderTicket>()
            .Property(t => t.TotalAmount)
            .HasColumnType("decimal(18,2)");

        // Seed dữ liệu 15 món ăn bằng HasData (bắt buộc theo đề, không dùng INSERT tay)
        modelBuilder.Entity<MenuItem>().HasData(
            new MenuItem { Id = 1, Code = "MON01", Name = "Cơm tấm sườn", Price = 35000, Unit = "phần", IsAvailable = true },
            new MenuItem { Id = 2, Code = "MON02", Name = "Cơm gà xối mỡ", Price = 32000, Unit = "phần", IsAvailable = true },
            new MenuItem { Id = 3, Code = "MON03", Name = "Bún bò Huế", Price = 30000, Unit = "tô", IsAvailable = true },
            new MenuItem { Id = 4, Code = "MON04", Name = "Bún chả cá", Price = 28000, Unit = "tô", IsAvailable = true },
            new MenuItem { Id = 5, Code = "MON05", Name = "Phở bò", Price = 33000, Unit = "tô", IsAvailable = true },
            new MenuItem { Id = 6, Code = "MON06", Name = "Mì Quảng", Price = 29000, Unit = "tô", IsAvailable = true },
            new MenuItem { Id = 7, Code = "MON07", Name = "Bánh mì thịt", Price = 18000, Unit = "ổ", IsAvailable = true },
            new MenuItem { Id = 8, Code = "MON08", Name = "Bánh xèo", Price = 25000, Unit = "cái", IsAvailable = true },
            new MenuItem { Id = 9, Code = "MON09", Name = "Cơm chiên dương châu", Price = 27000, Unit = "phần", IsAvailable = true },
            new MenuItem { Id = 10, Code = "MON10", Name = "Mì xào bò", Price = 30000, Unit = "phần", IsAvailable = true },
            new MenuItem { Id = 11, Code = "MON11", Name = "Trà đá", Price = 3000, Unit = "ly", IsAvailable = true },
            new MenuItem { Id = 12, Code = "MON12", Name = "Trà chanh", Price = 10000, Unit = "ly", IsAvailable = true },
            new MenuItem { Id = 13, Code = "MON13", Name = "Nước cam ép", Price = 15000, Unit = "ly", IsAvailable = true },
            new MenuItem { Id = 14, Code = "MON14", Name = "Sinh tố bơ", Price = 20000, Unit = "ly", IsAvailable = true },
            new MenuItem { Id = 15, Code = "MON15", Name = "Cà phê sữa đá", Price = 15000, Unit = "ly", IsAvailable = true }
        );
    }
}
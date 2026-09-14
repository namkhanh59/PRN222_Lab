using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FCanteen.Data;

// Class này CHỈ dùng để dotnet-ef biết cách khởi tạo DbContext lúc chạy migration.
// Vì FCanteen.Data là class library, không có Program.cs / DI container sẵn.
public class FCanteenContextFactory : IDesignTimeDbContextFactory<FCanteenContext>
{
    public FCanteenContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FCanteenContext>();

        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=PRN222_Lab_FCanteen;User Id=sa;Password=Namkhanh05092005!;TrustServerCertificate=True;MultipleActiveResultSets=true"
        );

        return new FCanteenContext(optionsBuilder.Options);
    }
}
using FCanteen.Data;
using FCanteen.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCanteen.PosClient.Services;

public class MenuService
{
    private readonly string _connectionString;

    public MenuService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<MenuItem>> GetAvailableMenuAsync()
    {
        var options = new DbContextOptionsBuilder<FCanteenContext>()
            .UseSqlServer(_connectionString)
            .Options;

        using var context = new FCanteenContext(options);
        return await context.MenuItems
            .Where(m => m.IsAvailable)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }
}
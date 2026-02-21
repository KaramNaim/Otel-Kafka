using Microsoft.EntityFrameworkCore;
using OTel.Domain.Models;

namespace OTel.Domain.Interfaces.Common;

public interface IAppDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

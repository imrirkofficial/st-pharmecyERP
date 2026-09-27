using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmacyERP2.Core.Interfaces;
using PharmacyERP2.Infrastructure.Data;
using PharmacyERP2.Infrastructure.Repositories;

namespace PharmacyERP2.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PharmacyDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}

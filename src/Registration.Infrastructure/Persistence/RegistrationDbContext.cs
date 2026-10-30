using MassTransit.EntityFrameworkCoreIntegration;

namespace Registration.Infrastructure.Persistence;

public sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : SagaDbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override IEnumerable<ISagaClassMap> Configurations => [new RegistrationStateMap()];

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}

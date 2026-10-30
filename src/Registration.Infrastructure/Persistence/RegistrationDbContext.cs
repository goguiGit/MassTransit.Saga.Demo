using MassTransit.EntityFrameworkCoreIntegration;

namespace Registration.Infrastructure.Persistence;

// One context for the saga table and the domain entities: the saga repository and the consumers that
// change users share the same database, so a single schema and a single EnsureCreated cover both.
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

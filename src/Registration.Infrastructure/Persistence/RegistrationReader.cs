using Registration.Application.Abstractions;

namespace Registration.Infrastructure.Persistence;

public sealed class RegistrationReader(RegistrationDbContext dbContext) : IRegistrationReader
{
    public Task<RegistrationState?> FindAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Set<RegistrationState>().AsNoTracking().SingleOrDefaultAsync(x => x.CorrelationId == userId, cancellationToken);
}

namespace Registration.Application.Abstractions;

public interface IRegistrationReader
{
    Task<RegistrationState?> FindAsync(Guid userId, CancellationToken cancellationToken = default);
}

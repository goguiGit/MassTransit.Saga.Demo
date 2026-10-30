namespace Registration.Application.Features.GetRegistration.Models;

/// <param name="SagaState">Null once the saga finished (user active or registration discarded).</param>
public sealed record RegistrationStatus(Guid UserId, string Email, UserStatus UserStatus, string? SagaState, DateTime CreatedAt, DateTime? ActivatedAt);

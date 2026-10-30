namespace Registration.Application.Features.GetRegistration.Models;

public sealed record RegistrationStatus(
    Guid UserId,
    string Email,
    UserStatus UserStatus,
    string? SagaState,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    DateTime? ExpiresAt);

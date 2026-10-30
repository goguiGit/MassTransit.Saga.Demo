using MassTransit;

namespace Registration.Domain.Models;

public sealed class RegistrationState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public Guid? ExpirationTokenId { get; set; }
    public DateTime RegisteredAt { get; set; }

    public void Start(string email, DateTime now)
    {
        Email = email;
        VerificationCode = Domain.VerificationCode.New();
        RegisteredAt = now;
    }

    public bool Accepts(string code) => code == VerificationCode;
}

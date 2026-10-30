using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Registration.Infrastructure.Persistence;

public sealed class RegistrationStateMap : SagaClassMap<RegistrationState>
{
    protected override void Configure(EntityTypeBuilder<RegistrationState> entity, ModelBuilder model)
    {
        entity.ToTable("RegistrationSagas");
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.Email).HasMaxLength(256);
        entity.Property(x => x.VerificationCode).HasMaxLength(VerificationCode.Length);
    }
}

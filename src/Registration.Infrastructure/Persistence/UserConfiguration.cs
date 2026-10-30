using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Registration.Infrastructure.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("Users");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
        entity.HasIndex(x => x.Email).IsUnique();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
    }
}

using System.Security.Cryptography;

namespace Registration.Domain;

public static class VerificationCode
{
    public const int Length = 6;

    public static string New() => RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
}

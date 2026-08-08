using System.Security.Cryptography;
using System.Text;

namespace BUnited.Modules.Billing.Infrastructure.Payments;

/// <summary>P3.07.a/b: HMAC-SHA256 signature over the raw request body, using a local demo
/// secret — this authenticates only our own server-generated fake events, never a real
/// provider's signing scheme.</summary>
public static class DemoWebhookSignature
{
    public static string Compute(string secret, string rawBody)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(string secret, string rawBody, string? providedSignature)
    {
        if (string.IsNullOrEmpty(providedSignature))
        {
            return false;
        }

        var expected = Compute(secret, rawBody);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(providedSignature);

        return expectedBytes.Length == providedBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

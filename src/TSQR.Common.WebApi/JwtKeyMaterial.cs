using System.Security.Cryptography;
using System.Text;

namespace TSQR.Common.WebApi;

/// <summary>
/// Resolves JWT key material from configuration. RSA public keys may be
/// supplied as raw PEM, base64-encoded PEM, or a file path.
/// </summary>
internal static class JwtKeyMaterial
{
    public const int MinimumRsaKeySizeBits = 2048;

    public static string? ResolvePublicKeyPem(
        string? raw,
        string? base64,
        string? path)
    {
        if (!string.IsNullOrWhiteSpace(raw))
            return NormalizePem(raw);

        if (!string.IsNullOrWhiteSpace(base64))
        {
            try
            {
                return NormalizePem(
                    Encoding.UTF8.GetString(Convert.FromBase64String(base64.Trim())));
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "Jwt:PublicKeyBase64 is not valid base64.",
                    ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            if (!File.Exists(path))
                throw new InvalidOperationException(
                    $"JWT public key file '{path}' was not found.");

            return NormalizePem(File.ReadAllText(path));
        }

        return null;
    }

    /// <summary>
    /// Parses a semicolon-separated key ring of <c>kid=base64Pem</c> entries.
    /// The returned RSA instances are owned by the caller and must be disposed.
    /// </summary>
    public static IReadOnlyDictionary<string, RSA> ParsePublicKeyRing(string value)
    {
        var keys = new Dictionary<string, RSA>(StringComparer.Ordinal);

        try
        {
            foreach (var entry in value.Split(
                         ';',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = entry.IndexOf('=');
                if (separator <= 0 || separator == entry.Length - 1)
                {
                    throw new InvalidOperationException(
                        "Jwt:PublicKeys entries must use the format 'kid=base64Pem' separated by semicolons.");
                }

                var kid = entry[..separator].Trim();
                var encodedPem = entry[(separator + 1)..].Trim();
                if (kid.Length == 0 || encodedPem.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Jwt:PublicKeys entries must include a non-empty key id and base64 PEM value.");
                }

                if (keys.ContainsKey(kid))
                {
                    throw new InvalidOperationException(
                        $"Jwt:PublicKeys contains the duplicate key id '{kid}'.");
                }

                string pem;
                try
                {
                    pem = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPem));
                }
                catch (FormatException ex)
                {
                    throw new InvalidOperationException(
                        $"Jwt:PublicKeys entry '{kid}' is not valid base64.",
                        ex);
                }

                keys[kid] = ImportRsaPublicKey(pem);
            }

            if (keys.Count == 0)
            {
                throw new InvalidOperationException(
                    "Jwt:PublicKeys must contain at least one 'kid=base64Pem' entry.");
            }

            return keys;
        }
        catch
        {
            foreach (var key in keys.Values)
                key.Dispose();
            throw;
        }
    }

    public static RSA ImportRsaPublicKey(string pem)
    {
        if (pem.Contains("PRIVATE KEY", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "JWT verifiers must be configured with a public key, not a private key.");

        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (Exception ex)
        {
            rsa.Dispose();
            throw new InvalidOperationException(
                "Jwt public key is not a valid PEM RSA public key.",
                ex);
        }

        if (rsa.KeySize < MinimumRsaKeySizeBits)
        {
            var keySize = rsa.KeySize;
            rsa.Dispose();
            throw new InvalidOperationException(
                $"JWT RSA public key must be at least {MinimumRsaKeySizeBits} bits (configured: {keySize}).");
        }

        return rsa;
    }

    private static string NormalizePem(string value) =>
        value.Replace("\\n", "\n", StringComparison.Ordinal).Trim();
}

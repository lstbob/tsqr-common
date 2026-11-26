using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace TSQR.Common.WebApi;

public static class TsqrJwtAuthenticationExtensions
{
    public const string DefaultIssuer = "tsqr-autheo";
    public const string DefaultAudience = "tsqr-services";
    public const string DefaultKeyId = "tsqr-autheo-v1";

    /// <summary>
    /// Registers the suite-wide JWT bearer authentication scheme using the
    /// standard <c>Jwt</c> configuration section. RS256 public-key validation
    /// is used when <c>Jwt:PublicKeys</c>, <c>Jwt:PublicKey</c> (or the
    /// base64/path variant) is configured; the legacy symmetric
    /// <c>Jwt:Key</c> remains supported.
    /// </summary>
    public static IServiceCollection AddTsqrJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtIssuer = jwtSection["Issuer"] ?? DefaultIssuer;
        var jwtAudience = jwtSection["Audience"] ?? DefaultAudience;
        var keyId = jwtSection["KeyId"] ?? DefaultKeyId;
        var publicKeysValue = jwtSection["PublicKeys"];
        var publicKeyPem = JwtKeyMaterial.ResolvePublicKeyPem(
            jwtSection["PublicKey"],
            jwtSection["PublicKeyBase64"],
            jwtSection["PublicKeyPath"]);

        SecurityKey validationKey;
        IssuerSigningKeyResolver? keyResolver = null;
        string[] validAlgorithms;

        if (!string.IsNullOrWhiteSpace(publicKeysValue))
        {
            var keyRing = JwtKeyMaterial.ParsePublicKeyRing(publicKeysValue);
            var securityKeys = keyRing
                .Select(pair => new RsaSecurityKey(pair.Value) { KeyId = pair.Key })
                .ToArray();
            var keysByKid = securityKeys.ToDictionary(
                key => key.KeyId!,
                key => key,
                StringComparer.Ordinal);

            validationKey = securityKeys[0];
            keyResolver = (_, _, kid, _) =>
                kid is not null && keysByKid.TryGetValue(kid, out var key)
                    ? [key]
                    : Array.Empty<SecurityKey>();
            validAlgorithms = [SecurityAlgorithms.RsaSha256];
        }
        else if (publicKeyPem is not null)
        {
            var rsa = JwtKeyMaterial.ImportRsaPublicKey(publicKeyPem);
            validationKey = new RsaSecurityKey(rsa) { KeyId = keyId };
            keyResolver = (_, _, kid, _) =>
                string.Equals(kid, keyId, StringComparison.Ordinal)
                    ? [validationKey]
                    : Array.Empty<SecurityKey>();
            validAlgorithms = [SecurityAlgorithms.RsaSha256];
        }
        else
        {
            var jwtKey = jwtSection["Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key or Jwt:PublicKey must be configured.");
            if (jwtKey.Length < 32)
                throw new InvalidOperationException(
                    "JWT symmetric key must be at least 32 characters.");
            validationKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            validAlgorithms = [SecurityAlgorithms.HmacSha256];
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // TLS terminates at the ingress/gateway; services listen on
                // plain HTTP inside the container network.
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = validationKey,
                    IssuerSigningKeyResolver = keyResolver,
                    ValidAlgorithms = validAlgorithms,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        return services;
    }
}

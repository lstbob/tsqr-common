using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TSQR.Common.WebApi.Tests;

public class JwtAuthenticationTests
{
    private const string Issuer = "tsqr-autheo";
    private const string Audience = "tsqr-tool-lib";
    private const string KeyId = "test-key-1";

    [Fact]
    public async Task Rs256Token_WithMatchingKid_ValidatesWithPublicKey()
    {
        using var rsa = RSA.Create(2048);
        var parameters = CreateParameters(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:PublicKey"] = rsa.ExportSubjectPublicKeyInfoPem(),
            ["Jwt:KeyId"] = KeyId,
        });
        var token = CreateRs256Token(rsa, KeyId);

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public async Task Rs256Token_WithWrongKid_IsRejected()
    {
        using var rsa = RSA.Create(2048);
        var parameters = CreateParameters(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:PublicKey"] = rsa.ExportSubjectPublicKeyInfoPem(),
            ["Jwt:KeyId"] = KeyId,
        });
        var token = CreateRs256Token(rsa, "different-key");

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Rs256Token_WithWrongAudience_IsRejected()
    {
        using var rsa = RSA.Create(2048);
        var parameters = CreateParameters(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:PublicKey"] = rsa.ExportSubjectPublicKeyInfoPem(),
            ["Jwt:KeyId"] = KeyId,
        });
        var token = CreateRs256Token(rsa, KeyId, audience: "another-service");

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Rs256Token_ForServiceA_IsRejectedByServiceB()
    {
        using var rsa = RSA.Create(2048);
        var serviceBParameters = CreateParameters(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = "tsqr-support",
            ["Jwt:PublicKey"] = rsa.ExportSubjectPublicKeyInfoPem(),
            ["Jwt:KeyId"] = KeyId,
        });
        var serviceAToken = CreateRs256Token(
            rsa,
            KeyId,
            audience: "tsqr-tool-lib");

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(
            serviceAToken,
            serviceBParameters);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Rs256PublicKey_CanBeSuppliedAsBase64()
    {
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var parameters = CreateParameters(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:PublicKeyBase64"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicPem)),
            ["Jwt:KeyId"] = KeyId,
        });
        var token = CreateRs256Token(rsa, KeyId);

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public async Task Rs256Token_SignedWithPreviousKey_ValidatesDuringRotation()
    {
        using var oldRsa = RSA.Create(2048);
        using var newRsa = RSA.Create(2048);
        var parameters = CreateMultiKeyParameters(
            ("test-key-1", oldRsa),
            ("test-key-2", newRsa));
        var token = CreateRs256Token(oldRsa, "test-key-1");

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public async Task Rs256Token_SignedWithNewKey_ValidatesDuringRotation()
    {
        using var oldRsa = RSA.Create(2048);
        using var newRsa = RSA.Create(2048);
        var parameters = CreateMultiKeyParameters(
            ("test-key-1", oldRsa),
            ("test-key-2", newRsa));
        var token = CreateRs256Token(newRsa, "test-key-2");

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public async Task Rs256Token_WithUnknownKid_IsRejectedByKeyRing()
    {
        using var knownRsa = RSA.Create(2048);
        using var unknownRsa = RSA.Create(2048);
        var parameters = CreateMultiKeyParameters(("test-key-1", knownRsa));
        var token = CreateRs256Token(unknownRsa, "unknown-key");

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Rs256Token_WithoutKid_IsRejectedByKeyRing()
    {
        using var rsa = RSA.Create(2048);
        var parameters = CreateMultiKeyParameters(("test-key-1", rsa));
        var token = CreateToken(
            new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256));

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void VerifierConfiguration_RejectsDuplicatePublicKeyIds()
    {
        using var rsa = RSA.Create(2048);
        var encodedPem = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(rsa.ExportSubjectPublicKeyInfoPem()));
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:PublicKeys"] = $"test-key-1={encodedPem};test-key-1={encodedPem}",
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddTsqrJwtAuthentication(configuration));

        Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifierConfiguration_RejectsMalformedPublicKeyRingEntry()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:PublicKeys"] = "missing-separator",
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddTsqrJwtAuthentication(configuration));

        Assert.Contains("kid=base64Pem", exception.Message);
    }

    [Fact]
    public async Task LegacyHmacToken_StillValidates()
    {
        const string key = "test-key-at-least-32-characters-long!!";
        var parameters = CreateParameters(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:Key"] = key,
        });
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var token = CreateToken(
            new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public void VerifierConfiguration_RejectsPrivateKeyMaterial()
    {
        using var rsa = RSA.Create(2048);
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:PublicKey"] = rsa.ExportPkcs8PrivateKeyPem(),
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddTsqrJwtAuthentication(configuration));

        Assert.Contains("public key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifierConfiguration_RejectsRsaKeysBelow2048Bits()
    {
        using var rsa = RSA.Create(1024);
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:PublicKey"] = rsa.ExportSubjectPublicKeyInfoPem(),
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddTsqrJwtAuthentication(configuration));

        Assert.Contains("2048", exception.Message);
    }

    [Fact]
    public void VerifierConfiguration_RejectsMalformedPublicKey()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:PublicKey"] = "not-a-pem-key",
        });

        Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddTsqrJwtAuthentication(configuration));
    }

    [Fact]
    public void VerifierConfiguration_RequiresAKey()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddTsqrJwtAuthentication(configuration));

        Assert.Contains("Jwt:PublicKey", exception.Message);
    }

    [Fact]
    public void VerifierConfiguration_RejectsShortSymmetricKey()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "too-short",
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddTsqrJwtAuthentication(configuration));

        Assert.Contains("32", exception.Message);
    }

    private static TokenValidationParameters CreateMultiKeyParameters(
        params (string KeyId, RSA Rsa)[] keys)
    {
        var entries = keys.Select(key =>
            $"{key.KeyId}={Convert.ToBase64String(Encoding.UTF8.GetBytes(key.Rsa.ExportSubjectPublicKeyInfoPem()))}");

        return CreateParameters(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:PublicKeys"] = string.Join(';', entries),
        });
    }

    private static TokenValidationParameters CreateParameters(
        Dictionary<string, string?> values)
    {
        var configuration = BuildConfiguration(values);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTsqrJwtAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        return provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters;
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static string CreateRs256Token(
        RSA rsa,
        string keyId,
        string audience = Audience)
    {
        var signingKey = new RsaSecurityKey(rsa) { KeyId = keyId };
        return CreateToken(
            new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256),
            audience);
    }

    private static string CreateToken(
        SigningCredentials credentials,
        string audience = Audience)
    {
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

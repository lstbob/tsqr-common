using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace TSQR.Common.WebApi;

public static class TsqrJwtAuthenticationExtensions
{
    /// <summary>
    /// Registers the suite-wide JWT bearer authentication scheme using the
    /// standard <c>Jwt</c> configuration section.
    /// </summary>
    public static IServiceCollection AddTsqrJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"]
            ?? throw new InvalidOperationException("JWT Key not configured.");
        var jwtIssuer = jwtSection["Issuer"] ?? "tsqr-autheo";
        var jwtAudience = jwtSection["Audience"] ?? "tsqr-services";

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        return services;
    }
}

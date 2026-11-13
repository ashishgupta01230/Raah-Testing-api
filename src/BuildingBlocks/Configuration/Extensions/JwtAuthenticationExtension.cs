using HautSphere.BuildingBlocks.Configuration.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HautSphere.BuildingBlocks.Configuration.Extensions
{
    public static class JwtAuthenticationExtension
    {
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<SessionContext>();
            var bindJwtSettings = new JwtSetting();
            configuration.Bind(nameof(JwtSetting), bindJwtSettings);
            services.AddSingleton(bindJwtSettings);

            var accessTokenCookieName = configuration["AuthCookie:AccessTokenCookieName"] ?? "accessToken";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuerSigningKey = bindJwtSettings.ValidateIssuerSigningKey,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(bindJwtSettings.IssuerSigningKey)),
                    ValidateIssuer = bindJwtSettings.ValidateIssuer,
                    ValidIssuer = bindJwtSettings.ValidIssuer,
                    ValidateAudience = bindJwtSettings.ValidateAudience,
                    ValidAudience = bindJwtSettings.ValidAudience,
                    RequireExpirationTime = bindJwtSettings.RequireExpirationTime,
                    ValidateLifetime = bindJwtSettings.ValidateLifetime,
                    ClockSkew = TimeSpan.FromDays(1),
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        // 1. Authorization: Bearer <token> header — highest priority
                        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                        {
                            var raw = authHeader.ToString();
                            if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = raw["Bearer ".Length..].Trim();
                            }
                        }

                        // 2. HttpOnly Cookie
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            context.Token = context.Request.Cookies[accessTokenCookieName];
                        }

                        // 3. Form body field "access_token"
                        if (string.IsNullOrEmpty(context.Token)
                            && context.Request.HasFormContentType
                            && context.Request.Form.TryGetValue("access_token", out var formToken))
                        {
                            context.Token = formToken;
                        }

                        await Task.CompletedTask;
                    }
                };
            });
        }
    }
}

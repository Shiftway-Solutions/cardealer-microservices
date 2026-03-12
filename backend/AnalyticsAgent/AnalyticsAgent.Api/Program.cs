using AnalyticsAgent.Application;
using AnalyticsAgent.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Secrets;

const string ServiceName = "AnalyticsAgent";
const string ServiceVersion = "1.0.0";

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.UseStandardSerilog(ServiceName);
    builder.Services.AddSecretProvider();
    builder.Services.AddStandardObservability(builder.Configuration, ServiceName, ServiceVersion);
    builder.Services.AddStandardErrorHandling(builder.Configuration, ServiceName);
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers().AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHttpContextAccessor();

    var (jwtKey, jwtIssuer, jwtAudience) = MicroserviceSecretsConfiguration.GetJwtConfig(builder.Configuration);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? (builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:3000", "http://localhost:5173" }
            : new[] { "https://okla.com.do", "https://www.okla.com.do" });
    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.WithOrigins(allowedOrigins).WithMethods("GET", "POST", "OPTIONS").WithHeaders("Content-Type", "Authorization").AllowCredentials()));

    var app = builder.Build();
    if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    Log.Information("{Service} v{Version} starting on port 8080", ServiceName, ServiceVersion);
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "{Service} terminated unexpectedly", ServiceName); }
finally { Log.CloseAndFlush(); }

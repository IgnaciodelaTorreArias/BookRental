using Microsoft.EntityFrameworkCore;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Commons.GrpcInterceptors;
using Commons.Auth.BearerToken;
using Commons.Kafka;
using InventoryService.Services.System;

using RentalService.DBContext;
using RentalService.Services.Operations;
using RentalService.Services.User;
using RentalService.Hosts;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
});
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("RentalService"))
    .WithLogging(logging => logging.AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
        .AddMeter("Microsoft.AspNetCore.Http.Connections")
        .AddMeter("Microsoft.Extensions.Diagnostics.ResourceMonitoring")
    )
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddGrpcClientInstrumentation()
    )
    .UseOtlpExporter(OpenTelemetry.Exporter.OtlpExportProtocol.Grpc, new Uri("http://localhost:4317"));

builder.Services.AddSingleton<ITokenService, MockTokenService>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        RSA credentials = RSA.Create();
        credentials.ImportFromPem(builder.Configuration.GetValue<string>("public_key"));
        options.TokenValidationParameters = new()
        {
            // typ
            ValidTypes = ["at+jwt"],
            // iss
            ValidIssuer = "http://localhost/BookRental",
            // aud
            ValidAudience = "http://localhost/BookRental",

            IssuerSigningKey = new RsaSecurityKey(credentials),
            ValidateIssuerSigningKey = true
        };
    });
builder.Services.AddDbContextPool<RentalContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Rental"));
});
builder.Services.AddGrpcClient<SBookLend.SBookLendClient>(o =>
{
    o.Address = new Uri("https://localhost:5501");
});
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ExceptionInterceptor>();
});
builder.Services.AddHostedService<KafkaConsumer>();
builder.Services.AddHostedService<BookCopyReassignements>();

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<RentalOperations>();
app.MapGrpcService<Rental>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

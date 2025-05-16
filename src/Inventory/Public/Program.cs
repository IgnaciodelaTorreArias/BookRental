using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Microsoft.EntityFrameworkCore;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Commons.Kafka;
using Commons.GrpcInterceptors;

using Inventory.DBContext;

using Inventory.Public.Services.Consumer;
using Inventory.Public.Services.Administration;
using Inventory.Public.Hosts;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AddConsoleExporter();
});
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("InventoryService"))
    .WithLogging(logging => logging.AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
        .AddMeter("Microsoft.AspNetCore.Http.Connections")
        .AddMeter("Microsoft.Extensions.Diagnostics.ResourceMonitoring")
    )
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
    )
    .UseOtlpExporter(OpenTelemetry.Exporter.OtlpExportProtocol.Grpc, new Uri("http://localhost:4317"));
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        RSA credentials = RSA.Create();
        credentials.ImportFromPem(File.ReadAllText("PublicKey.pem"));
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
builder.Services.AddAuthorization();
builder.Services.AddDbContextPool<InventoryContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Inventory"),
        o => o.MapEnum<Inventory.DBContext.Models.Acquisition.AcquisitionStatus>("acquisition_status")
            .MapEnum<Inventory.DBContext.Models.Stock.CopyStatus>("copy_status")
    );
});
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ExceptionInterceptor>();
});
builder.Services.AddHostedService<KafkaConsumer>();

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<InventoryConsumer>();
app.MapGrpcService<InventoryAdministration>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

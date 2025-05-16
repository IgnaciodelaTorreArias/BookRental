using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

using Inventory.Internal.Services;

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
    options.AddConsoleExporter();
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
builder.Services.AddDbContextPool<RentalContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Rental"));
});
var certificate = X509CertificateLoader.LoadPkcs12FromFile("Rental.pfx", builder.Configuration["Kestrel:Endpoints:Rental:Certificate:Password"]);
var caCert = X509CertificateLoader.LoadCertificateFromFile("BookRentalCA.crt");
builder.Services.AddGrpcClient<SInventorySystem.SInventorySystemClient>(o =>
{
    o.Address = new Uri("https://localhost:5502");
    o.ChannelOptionsActions.Add(channelOptions =>
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(certificate);
        handler.ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
        {
            if (cert == null)
                return false;
            var customChain = new X509Chain();
            customChain.ChainPolicy.ExtraStore.Add(caCert);
            customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            if (!customChain.Build(cert))
                return false;
            return customChain.ChainElements.Any(c => c.Certificate.Equals(caCert));
        };
        channelOptions.HttpHandler = handler;
    });
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

using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using SIAdministration = InventoryService.Services.Administration;
using SIUser = InventoryService.Services.Users;
using SROperations = RentalService.Services.Operations;
using SRUser = RentalService.Services.User;

using APIGateway.Filters;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
});
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("APIGateway"))
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


builder.Services.AddGrpcClient<SIAdministration.SInvenotryAdministration.SInvenotryAdministrationClient>(o =>
{
    o.Address = new Uri("https://localhost:5501"); // TODO: Change for services names when all services are fully dockerized
});
builder.Services.AddGrpcClient<SIUser.SInventory.SInventoryClient>(o =>
{
    o.Address = new Uri("https://localhost:5501");
});
builder.Services.AddGrpcClient<SROperations.SRentalOperations.SRentalOperationsClient>(o =>
{
    o.Address = new Uri("https://localhost:5502");
});
builder.Services.AddGrpcClient<SRUser.SRental.SRentalClient>(o =>
{
    o.Address = new Uri("https://localhost:5502");
});

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

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
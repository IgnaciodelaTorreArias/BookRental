using Microsoft.EntityFrameworkCore;

using Commons.GrpcInterceptors;

using Inventory.DBContext;

using Inventory.Internal.Services;
using System.Security.Cryptography.X509Certificates;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var caCert = X509CertificateLoader.LoadCertificateFromFile("BookRentalCA.crt");
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.CheckCertificateRevocation = false;
        httpsOptions.ClientCertificateValidation = (cert, _, _) => {
            var customChain = new X509Chain();
            customChain.ChainPolicy.ExtraStore.Add(caCert);
            customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            if (!customChain.Build(cert))
                return false;
            return customChain.ChainElements.Any(c => c.Certificate.Equals(caCert));
        };
    });
});
builder.Services.AddGrpc();
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

var app = builder.Build();

app.MapGrpcService<InventorySystem>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

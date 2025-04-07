using Commons.Kafka;

using InventoryService.DBContext;

namespace InventoryService.Services.Administration;

public partial class InventoryAdministration(
    InventoryContext context,
    IKafkaProducer producer,
    ILogger<InventoryAdministration> logger
) : SInvenotryAdministration.SInvenotryAdministrationBase
{
    private readonly InventoryContext _context = context;
    private readonly IKafkaProducer _producer = producer;
    public readonly ILogger<InventoryAdministration> _logger = logger;
}
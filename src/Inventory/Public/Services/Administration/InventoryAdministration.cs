using Qdrant.Client;

using Commons.Kafka;

using Inventory.DBContext;

using Inventory.Public.Models.Sentences;

namespace Inventory.Public.Services.Administration;

public partial class InventoryAdministration(
    InventoryContext context,
    IKafkaProducer producer,
    ILogger<InventoryAdministration> logger,
    QdrantClient qdrant,
    SentencesModel sentencesModel
) : SInvenotryAdministration.SInvenotryAdministrationBase
{
    private readonly InventoryContext _context = context;
    private readonly IKafkaProducer _producer = producer;
    public readonly ILogger<InventoryAdministration> _logger = logger;
    private readonly QdrantClient _qdrant = qdrant;
    private readonly SentencesModel _sentencesModel = sentencesModel;
}
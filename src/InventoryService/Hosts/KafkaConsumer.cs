using Confluent.Kafka;

using Commons.Kafka;
using InventoryService.DBContext;
using InventoryService.DBContext.Models;

namespace InventoryService.Hosts;

public class KafkaConsumer : BackgroundService
{
    private readonly IConsumer<Ignore, byte[]> _consumer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<KafkaConsumer> _logger;
    public KafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<KafkaConsumer> logger
    )
    {
        ConsumerConfig config = new();
        configuration.GetSection("Kafka:Consumer").Bind(config);
        config.EnableAutoCommit = false;
        config.AutoOffsetReset = AutoOffsetReset.Earliest;
        _consumer = new ConsumerBuilder<Ignore, byte[]>(config).Build();
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("book_returned");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ConsumeResult<Ignore, byte[]> kafkaEvent = await Task.Run(() => _consumer.Consume(stoppingToken));
                KafkaBook book = KafkaBook.Parser.ParseFrom(kafkaEvent.Message.Value);
                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    InventoryContext? _context = scope.ServiceProvider.GetService<InventoryContext>();
                    if (_context is null)
                        throw new InvalidOperationException("InventoryContext is not available.");
                    Stock bookCopy = new()
                    {
                        CopyId = (int)book.Identifier,
                        Status = Stock.CopyStatus.Unavailable
                    };
                    _context.Stocks.Attach(bookCopy);
                    bookCopy.Status = Stock.CopyStatus.Available;
                    await _context.SaveChangesAsync(stoppingToken);
                }
                _consumer.Commit(kafkaEvent);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred: {@Error}", new
                {
                    Event = "KafkaConsumerException",
                    ex.Message,
                    ex.Source,
                    ex.StackTrace,
                    ex.TargetSite
                });
                try
                {
                    await Task.Delay(60_000, stoppingToken); // 1 minute recovery
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        _consumer.Close();
    }
    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}

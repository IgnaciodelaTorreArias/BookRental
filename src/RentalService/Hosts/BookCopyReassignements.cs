using Microsoft.EntityFrameworkCore;
using Google.Protobuf;

using Commons.Kafka;

using RentalService.DBContext;

namespace RentalService.Hosts;

public class BookCopyReassignements(
    IServiceScopeFactory serviceScopeFactory,
    IKafkaProducer producer,
    ILogger<BookCopyReassignements> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IKafkaProducer _producer = producer;
    private readonly ILogger<BookCopyReassignements> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                RentalContext? _context = scope.ServiceProvider.GetService<RentalContext>();
                if (_context is null)
                    throw new InvalidOperationException("RentalContext is not available.");
                int? copyId = await _context.Database.SqlQuery<int?>(
                    $"SELECT rental.reassign_expired_notification() AS \"Value\""
                ).FirstAsync();
                if (!copyId.HasValue)
                {
                    await Task.Delay(300_000_000, stoppingToken); // 5 minutes
                    continue;
                }
                if (copyId.Value < 0)
                {
                    await _producer.ProduceAsync("book_returned",
                    new KafkaBook()
                    {
                        Identifier = (uint)(-copyId.Value)
                    }.ToByteArray()
                    );
                }
                else
                {
                    DBContext.Models.Notified record = await _context.Notifieds.FirstAsync(record => record.CopyId == copyId);
                    await _producer.ProduceAsync("pending_confirmation",
                        new KafkaRental()
                        {
                            BookId = (uint)record.BookId,
                            CopyId = (uint)copyId,
                            UserId = (uint)record.UserId
                        }.ToByteArray()
                    );
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred: {@Error}", new
                {
                    Event = "BookCopyReassignements",
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
    }
}

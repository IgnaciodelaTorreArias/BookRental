using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf;

using Commons.Kafka;
using Commons.Auth.BearerToken;
using InventoryService.Services.System;

using RentalService.DBContext;
using RentalService.DBContext.Models;

namespace RentalService.Hosts;

public class KafkaConsumer : BackgroundService
{
    private readonly IConsumer<Ignore, byte[]> _consumer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITokenService _tokenService;
    private readonly SBookLend.SBookLendClient _client;
    private readonly IKafkaProducer _producer;
    private readonly ILogger _logger;

    public KafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory,
        ITokenService tokenService,
        SBookLend.SBookLendClient client,
        IKafkaProducer producer,
        ILogger<KafkaConsumer> logger
    )
    {
        ConsumerConfig config = new()
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };
        configuration.GetSection("Kafka:Consumer").Bind(config);
        config.EnableAutoCommit = false;
        config.AutoOffsetReset = AutoOffsetReset.Earliest;
        _consumer = new ConsumerBuilder<Ignore, byte[]>(config).Build();
        _serviceScopeFactory = serviceScopeFactory;
        _tokenService = tokenService;
        _client = client;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(["book_management", "book_available"]);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ConsumeResult<Ignore, byte[]> kafkaEvent = await Task.Run(() => _consumer.Consume(stoppingToken));
                KafkaBook book = KafkaBook.Parser.ParseFrom(kafkaEvent.Message.Value);
                switch (kafkaEvent.Topic)
                {
                    case "book_management":
                        if (book.Operation == BookOperation.Created)
                            await BookCreated(book);
                        if (book.Operation == BookOperation.Updated)
                            await BookUpdated(book);
                        break;
                    case "book_available":
                        await AvailableBooks(book);
                        break;
                    default:
                        break;
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

    public async Task BookCreated(KafkaBook book)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        RentalContext? _context = scope.ServiceProvider.GetService<RentalContext>();
        if (_context is null)
            throw new InvalidOperationException("RentalContext is not available.");
        Book newBook = new()
        {
            BookId = (int)book.Identifier,
            RentalFee = (int)book.RentalFee,
            Visible = book.Visible
        };
        _context.Books.Add(newBook);
        await _context.SaveChangesAsync();
    }

    public async Task BookUpdated(KafkaBook book)
    {
        if (!book.HasRentalFee && !book.HasVisible)
            return;
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        RentalContext? _context = scope.ServiceProvider.GetService<RentalContext>();
        if (_context is null)
            throw new InvalidOperationException("RentalContext is not available.");
        Book newBook = new()
        {
            BookId = (int)book.Identifier,
        };
        if (book.HasRentalFee)
            newBook.RentalFee = ((int)book.RentalFee) - 1;
        if (book.HasVisible)
            newBook.Visible = !book.Visible;
        _context.Books.Attach(newBook);
        if (book.HasRentalFee)
            newBook.RentalFee = (int)book.RentalFee;
        if (book.HasVisible)
            newBook.Visible = book.Visible;
        await _context.SaveChangesAsync();
    }

    public async Task AvailableBooks(KafkaBook book)
    {
        while (true)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            string token = await _tokenService.GetTokenAsync();
            Commons.Messages.Resources.ResourceIdentifier result = await _client.ReserveBookAsync(new() { Identifier = book.Identifier }, new Grpc.Core.Metadata() {
                { "Authorization", $"Bearer {token}" }
            });
            if (result.Identifier == 0)
                return;
            RentalContext? _context = scope.ServiceProvider.GetService<RentalContext>();
            if (_context is null)
                throw new InvalidOperationException("RentalContext is not available.");
            int? user = await _context.Database.SqlQuery<int?>(
                $"SELECT rental.get_user_to_notify({(int)book.Identifier},{(int)result.Identifier}) AS \"Value\""
            ).FirstOrDefaultAsync();
            if (!user.HasValue)
            {
                await _producer.ProduceAsync("book_returned",
                    new KafkaBook()
                    {
                        Identifier = result.Identifier
                    }.ToByteArray()
                );
                return;
            }
            else
                await _producer.ProduceAsync("pending_confirmation",
                    new KafkaRental()
                    {
                        BookId = book.Identifier,
                        CopyId = result.Identifier,
                        UserId = (uint)user
                    }.ToByteArray()
                );
        }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}

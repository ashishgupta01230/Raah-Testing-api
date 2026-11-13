using Binsera.Core.OutBox.Domain;
using Binsera.Persistence;
using HautSphere.BuildingBlocks.Application;
using HautSphere.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable disable
namespace Binsera.API.BackgroundServices
{
    public class OutboxPublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisher> _logger;
        private readonly TimeSpan _pollDelay = TimeSpan.FromSeconds(5);
        private readonly int _maxAttempts = 5;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        public OutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<BinseraDbContext>();

                    // fetch a batch of unprocessed messages
                    var batch = await db.Set<OutboxMessage>()
                        .Where(x => !x.Processed && !x.DeadLetter)
                        .OrderBy(x => x.OccurredOn)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    foreach (var msg in batch)
                    {
                        try
                        {
                            var type = Type.GetType(msg.MessageType);
                            if (type == null)
                            {
                                msg.Attempts++;
                                msg.LastAttemptOn = DateTime.UtcNow;
                                msg.Error = $"Unknown message type: {msg.MessageType}";
                                if (msg.Attempts >= _maxAttempts) msg.DeadLetter = true;
                                db.Update(msg);
                                await db.SaveChangesAsync(stoppingToken);
                                continue;
                            }

                            var evt = (IDomainEvent)JsonSerializer.Deserialize(msg.Payload, type, _jsonOptions);
                            // find all external handlers
                            var handlerType = typeof(IExternalEventHandler<>).MakeGenericType(type);
                            var handlers = (IEnumerable<object>)scope.ServiceProvider.GetService(typeof(IEnumerable<>)
                                .MakeGenericType(handlerType))
                                            ?? Enumerable.Empty<object>();

                            foreach (dynamic handler in handlers)
                            {
                                await handler.HandleAsync((dynamic)evt, stoppingToken);
                            }

                            // success
                            msg.Processed = true;
                            msg.ProcessedOn = DateTime.UtcNow;
                            db.Update(msg);
                            await db.SaveChangesAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            // increment attempt, set last attempt, maybe dead-letter
                            msg.Attempts++;
                            msg.LastAttemptOn = DateTime.UtcNow;
                            msg.Error = ex.ToString();
                            if (msg.Attempts >= _maxAttempts) msg.DeadLetter = true;
                            db.Update(msg);
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogWarning(ex, "Outbox message processing failed. MessageId: {Id}, Attempts: {Attempts}", msg.Id, msg.Attempts);
                        }

                        if (stoppingToken.IsCancellationRequested) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OutboxPublisher error");
                }

                await Task.Delay(_pollDelay, stoppingToken);
           }
        }
    }

}

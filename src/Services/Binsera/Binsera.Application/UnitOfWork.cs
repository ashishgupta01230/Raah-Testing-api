using Binsera.Core.OutBox.Domain;
using Binsera.Persistence;
using HautSphere.BuildingBlocks.Application;
using HautSphere.BuildingBlocks.Domain;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable disable
namespace Binsera.Application
{
    // UnitOfWork.cs
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        private readonly BinseraDbContext _db;
        private readonly IServiceProvider _serviceProvider; // used to resolve handlers
        private IDbContextTransaction _dbTransaction;

        public UnitOfWork(BinseraDbContext db, IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
        }

        public async Task StartTransactionAsync(CancellationToken ct = default)
        {
            if (_dbTransaction != null) return;
            _dbTransaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            if (_dbTransaction == null) return;

            try
            {
                // 1-3) Drain domain events in passes, not a single snapshot. A handler can
                // itself mutate another aggregate and raise a new domain event (e.g.
                // OrderApprovedDomainEventHandler calling member.UpdatePayNo(1), which adds
                // MemberPayNoUpdatedDomainEvent to the Member aggregate). Collecting the
                // aggregate list once up front missed any such nested event entirely, so the
                // "member approved" side effect silently never ran. Looping until no aggregate
                // has a pending event ensures nested events raised mid-commit are processed too.
                var safetyLimit = 20; // guards against a handler that keeps re-raising events
                while (safetyLimit-- > 0)
                {
                    var pending = _db.ChangeTracker
                        .Entries<DomainBase>()
                        .Select(e => e.Entity)
                        .Where(a => a.DomainEvents != null && a.DomainEvents.Any())
                        .ToList();

                    if (pending.Count == 0)
                        break;

                    foreach (var agg in pending)
                    {
                        // Drain this aggregate's events before dispatching them, so any new
                        // event a handler raises on it lands in the next pass rather than
                        // being lost or re-processed forever.
                        var events = agg.DomainEvents.ToList();
                        agg.ClearDomainEvents();

                        foreach (var ev in events)
                        {
                            // internal handlers, run in-transaction
                            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(ev.GetType());
                            var handlers = (IEnumerable<object>)_serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType))
                                           ?? Enumerable.Empty<object>();

                            foreach (dynamic handler in handlers)
                            {
                                await handler.HandleAsync((dynamic)ev, ct);
                            }

                            // external handlers -> Outbox messages
                            var externalHandlerType = typeof(IExternalEventHandler<>).MakeGenericType(ev.GetType());
                            var externalHandlers = (IEnumerable<object>)_serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(externalHandlerType))
                                           ?? Enumerable.Empty<object>();

                            if (!externalHandlers.Any()) continue;

                            var outbox = new OutboxMessage
                            {
                                OccurredOn = DateTime.Now,
                                MessageType = ev.GetType().AssemblyQualifiedName,
                                Payload = JsonSerializer.Serialize(ev, ev.GetType(), _jsonOptions)
                            };
                            _db.Set<OutboxMessage>().Add(outbox);
                        }
                    }
                }

                // 4) Save all changes (aggregate updates + Outbox rows) inside transaction
                await _db.SaveChangesAsync(ct);

                // 5) Commit transaction
                await _dbTransaction.CommitAsync(ct);
            }
            catch
            {
                try { if (_dbTransaction != null) await _dbTransaction.RollbackAsync(); } catch { /* log */ }
                throw;
            }
            finally
            {
                if (_dbTransaction != null) await _dbTransaction.DisposeAsync();
                _dbTransaction = null;

                // clear domain events in tracked aggregates
                foreach (var agg in _db.ChangeTracker.Entries<DomainBase>().Select(e => e.Entity))
                    agg.ClearDomainEvents();
            }
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            if (_dbTransaction == null) return;
            try
            {
                await _dbTransaction.RollbackAsync(ct);
            }
            finally
            {
                await _dbTransaction.DisposeAsync();
                _dbTransaction = null;
            }
        }

        public IRepository<T> Repository<T>() where T : DomainBase
        {
            // implement your repository provider
            return new Repository<T>(_db);
        }

        public async ValueTask DisposeAsync()
        {
            if (_dbTransaction != null)
            {
                await _dbTransaction.RollbackAsync();
                await _dbTransaction.DisposeAsync();
                _dbTransaction = null;
            }
            // DO NOT dispose _db if it is DI-scoped; DI will handle it.
        }
    }

}

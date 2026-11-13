using HautSphere.BuildingBlocks.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace HautSphere.BuildingBlocks.Application.EventDispatcher
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public DomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync(IDomainEvent domainEvent)
        {
            //find all handlers for this event type
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);
            if (handlers!= null && handlers.Any())
            {
                foreach (dynamic handler in handlers)
                {
                    await handler.HandleAsync((dynamic)domainEvent);
                }
            }

        }
    }
}

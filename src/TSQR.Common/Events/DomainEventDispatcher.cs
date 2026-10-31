using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace TSQR.Common.Events;

/// <summary>
/// Resolves every <see cref="IDomainEventHandler{TEvent}"/> registered for an
/// event's runtime type and invokes the handlers sequentially.
/// </summary>
/// <remarks>
/// Handler lookup and invocation metadata are cached per event type. The
/// compiled invoker avoids a reflection call for every event while preserving
/// the original exception instead of wrapping it in
/// <see cref="System.Reflection.TargetInvocationException"/>.
///
/// Handlers run sequentially on purpose: they normally share the scoped
/// unit-of-work / DbContext and are not safe to execute concurrently.
/// </remarks>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            var invoker = Invokers.GetOrAdd(domainEvent.GetType(), CreateInvoker);

            if (serviceProvider.GetService(invoker.HandlersType) is not IEnumerable handlers)
                continue;

            foreach (var handler in handlers)
            {
                if (handler is null)
                    continue;

                await invoker.Handler(handler, domainEvent, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static HandlerInvoker CreateInvoker(Type eventType)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlersType = typeof(IEnumerable<>).MakeGenericType(handlerType);
        var handleMethod = handlerType.GetMethod("HandleAsync")
            ?? throw new InvalidOperationException(
                $"Could not find HandleAsync on {handlerType.FullName}.");

        var handlerParameter = Expression.Parameter(typeof(object), "handler");
        var eventParameter = Expression.Parameter(typeof(IDomainEvent), "domainEvent");
        var cancellationTokenParameter = Expression.Parameter(
            typeof(CancellationToken), "cancellationToken");

        var call = Expression.Call(
            Expression.Convert(handlerParameter, handlerType),
            handleMethod,
            Expression.Convert(eventParameter, eventType),
            cancellationTokenParameter);

        var handler = Expression
            .Lambda<Func<object, IDomainEvent, CancellationToken, Task>>(
                call,
                handlerParameter,
                eventParameter,
                cancellationTokenParameter)
            .Compile();

        return new HandlerInvoker(handlersType, handler);
    }

    private sealed record HandlerInvoker(
        Type HandlersType,
        Func<object, IDomainEvent, CancellationToken, Task> Handler);
}

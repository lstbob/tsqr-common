using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace TSQR.Common.Events;

/// <summary>
/// Resolves every handler registered for an event's runtime type and invokes
/// the handlers sequentially.
/// </summary>
/// <remarks>
/// Handler lookup and invocation metadata are cached per event type and
/// dispatch phase. The compiled invoker avoids a reflection call for every
/// event while preserving the original exception instead of wrapping it in
/// <see cref="System.Reflection.TargetInvocationException"/>.
///
/// Handlers run sequentially on purpose: in-transaction handlers normally
/// share the scoped unit-of-work / DbContext and are not safe to execute
/// concurrently.
/// </remarks>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<HandlerCacheKey, HandlerInvoker> Invokers = new();

    public ValueTask DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default) =>
        DispatchCoreAsync(domainEvents, typeof(IDomainEventHandler<>), cancellationToken);

    public ValueTask DispatchPostCommitAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default) =>
        DispatchCoreAsync(domainEvents, typeof(IPostCommitDomainEventHandler<>), cancellationToken);

    private async ValueTask DispatchCoreAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        Type openHandlerType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            var invoker = Invokers.GetOrAdd(
                new HandlerCacheKey(domainEvent.GetType(), openHandlerType),
                static key => CreateInvoker(key));

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

    private static HandlerInvoker CreateInvoker(HandlerCacheKey key)
    {
        var handlerType = key.OpenHandlerType.MakeGenericType(key.EventType);
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
            Expression.Convert(eventParameter, key.EventType),
            cancellationTokenParameter);

        var handler = Expression
            .Lambda<Func<object, IDomainEvent, CancellationToken, ValueTask>>(
                call,
                handlerParameter,
                eventParameter,
                cancellationTokenParameter)
            .Compile();

        return new HandlerInvoker(handlersType, handler);
    }

    private readonly record struct HandlerCacheKey(Type EventType, Type OpenHandlerType);

    private sealed record HandlerInvoker(
        Type HandlersType,
        Func<object, IDomainEvent, CancellationToken, ValueTask> Handler);
}

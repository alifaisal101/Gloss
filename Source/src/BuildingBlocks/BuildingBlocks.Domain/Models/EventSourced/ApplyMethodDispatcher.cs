using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Domain.Models.EventSourced;

internal static class ApplyMethodDispatcher
{
    private static readonly ConcurrentDictionary<Type, Dictionary<Type, Action<object, IDomainEvent>>> Cache = new();

    internal static void Dispatch(object aggregate, IDomainEvent @event)
    {
        var handlers = Cache.GetOrAdd(aggregate.GetType(), BuildHandlerMap);

        if (handlers.TryGetValue(@event.GetType(), out var handler))
            handler(aggregate, @event);
    }

    [SuppressMessage("SonarAnalyzer.CSharp", "S3011:Make sure that this accessibility bypass is safe here",
        Justification = "NonPublic is required to discover private [EventHandler] methods on aggregates. Access is constrained to methods explicitly opted in via the attribute.")]
    private static Dictionary<Type, Action<object, IDomainEvent>> BuildHandlerMap(Type aggregateType)
    {
        var result = new Dictionary<Type, Action<object, IDomainEvent>>();

        foreach (var method in aggregateType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (!Attribute.IsDefined(method, typeof(EventHandlerAttribute))) continue;

            var parameters = method.GetParameters();
            if (parameters.Length != 1) continue;
            if (!typeof(IDomainEvent).IsAssignableFrom(parameters[0].ParameterType)) continue;
            if (method.ReturnType != typeof(void)) continue;

            var eventType = parameters[0].ParameterType;
            result[eventType] = Compile(aggregateType, method, eventType);
        }

        return result;
    }

    private static Action<object, IDomainEvent> Compile(Type aggregateType, MethodInfo method, Type eventType)
    {
        var aggregateParam = Expression.Parameter(typeof(object), "aggregate");
        var eventParam = Expression.Parameter(typeof(IDomainEvent), "@event");

        var call = Expression.Call(
            Expression.Convert(aggregateParam, aggregateType),
            method,
            Expression.Convert(eventParam, eventType));

        return Expression.Lambda<Action<object, IDomainEvent>>(
            call, aggregateParam, eventParam).Compile();
    }
}

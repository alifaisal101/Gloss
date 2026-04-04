namespace BuildingBlocks.Domain.Models.EventSourced;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EventHandlerAttribute : Attribute;
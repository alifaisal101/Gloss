namespace BuildingBlocks.Application.EventSourcing;

public interface IEventTypeRegistry
{
    Type Resolve(string name);
    string GetName(Type type);
}

namespace BuildingBlocks.Domain.Models.EventSourced;

public interface ISnapshotable
{
    object TakeSnapshot();
    void RestoreFromSnapshot(object snapshot);
}

public interface ISnapshotable<TSnapshot> : ISnapshotable
{
    new TSnapshot TakeSnapshot();
    void RestoreFromSnapshot(TSnapshot snapshot);

    object ISnapshotable.TakeSnapshot() => TakeSnapshot()!;
    void ISnapshotable.RestoreFromSnapshot(object snapshot) => RestoreFromSnapshot((TSnapshot)snapshot);
}
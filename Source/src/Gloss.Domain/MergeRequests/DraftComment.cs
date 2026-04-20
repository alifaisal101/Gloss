using BuildingBlocks.Domain.Models;

namespace Gloss.Domain.MergeRequests;

public sealed class DraftComment : AggregateRoot<Guid>
{
    public Guid MergeRequestId { get; private set; }
    public string FilePath { get; private set; } = null!;
    public int Line { get; private set; }
    public string Body { get; private set; } = null!;
    public string? Reasoning { get; private set; }
    public DraftCommentState State { get; private set; }

    private DraftComment() : base(Guid.NewGuid()) { }

    public static DraftComment Create(Guid mergeRequestId, string filePath, int line, string body, string? reasoning,
        DraftCommentState state = DraftCommentState.Generated)
    {
        var dc = new DraftComment();
        dc.MergeRequestId = mergeRequestId;
        dc.FilePath = filePath;
        dc.Line = line;
        dc.Body = body;
        dc.Reasoning = reasoning;
        dc.State = state;
        return dc;
    }

    public void Update(string filePath, int line, string body, string? reasoning)
    {
        FilePath = filePath;
        Line = line;
        Body = body;
        Reasoning = reasoning;
        State = DraftCommentState.Edited;
    }
}

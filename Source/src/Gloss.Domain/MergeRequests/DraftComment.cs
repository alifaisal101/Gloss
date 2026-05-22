using BuildingBlocks.Domain.Models;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests.BusinessRules;

namespace Gloss.Domain.MergeRequests;

public sealed class DraftComment : AggregateRoot<Guid>
{
    public Guid MrReviewId { get; private set; }
    public string FilePath { get; private set; } = null!;
    public int Line { get; private set; }
    public string Body { get; private set; } = null!;
    public string? Reasoning { get; private set; }
    public DraftCommentState State { get; private set; }

    private DraftComment() : base(Guid.NewGuid()) { }

    public static Result<DraftComment> Create(
        Guid mrReviewId,
        string filePath,
        int line,
        string body,
        string? reasoning,
        DraftCommentState state = DraftCommentState.Generated)
    {
        var bodyRule = CheckRule(new DraftCommentBodyNotEmpty(body));
        if (bodyRule.IsFailure) return bodyRule.Error;

        var filePathRule = CheckRule(new DraftCommentFilePathNotEmpty(filePath));
        if (filePathRule.IsFailure) return filePathRule.Error;

        var lineRule = CheckRule(new DraftCommentLineValid(line));
        if (lineRule.IsFailure) return lineRule.Error;

        var dc = new DraftComment
        {
            MrReviewId = mrReviewId,
            FilePath = filePath,
            Line = line,
            Body = body,
            Reasoning = reasoning,
            State = state,
        };
        return dc;
    }

    public VoidResult Update(string filePath, int line, string body, string? reasoning)
    {
        var bodyRule = CheckRule(new DraftCommentBodyNotEmpty(body));
        if (bodyRule.IsFailure) return bodyRule.Error;

        var filePathRule = CheckRule(new DraftCommentFilePathNotEmpty(filePath));
        if (filePathRule.IsFailure) return filePathRule.Error;

        var lineRule = CheckRule(new DraftCommentLineValid(line));
        if (lineRule.IsFailure) return lineRule.Error;

        FilePath = filePath;
        Line = line;
        Body = body;
        Reasoning = reasoning;
        State = DraftCommentState.Edited;
        return Result.Success();
    }
}

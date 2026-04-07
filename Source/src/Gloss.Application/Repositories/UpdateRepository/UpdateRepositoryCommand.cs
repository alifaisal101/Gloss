using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Gloss.Application.Repositories.UpdateRepository;

public sealed record UpdateRepositoryCommand(
    [property: JsonIgnore] [property: ValidateNever] Guid RepositoryId,
    string? PollCron,
    bool? AutoReviewEnabled);

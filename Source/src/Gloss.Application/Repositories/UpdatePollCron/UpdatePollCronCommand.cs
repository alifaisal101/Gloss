using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Gloss.Application.Repositories.UpdatePollCron;

public sealed record UpdatePollCronCommand([property: JsonIgnore] [property: ValidateNever] Guid RepositoryId, string PollCron);

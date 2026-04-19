using System.Diagnostics.CodeAnalysis;

namespace Gloss.Infrastructure.Reviews.Anthropic;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Discriminated union base type")]
[SuppressMessage("Major Code Smell", "S4023:Interfaces should not be empty", Justification = "Discriminated union base type")]
internal interface IClaudeContent { }

namespace Gloss.IntegrationTests;

public sealed class AgenticGlossApiFactory : GlossApiFactory
{
    protected override bool UseRealReviewProvider => true;
}

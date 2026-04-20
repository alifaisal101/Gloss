using BuildingBlocks.Infrastructure.Api;
using BuildingBlocks.Infrastructure.Api.Documentation;
using BuildingBlocks.Infrastructure.Api.Health;
using BuildingBlocks.Infrastructure.Api.Jobs;
using BuildingBlocks.Infrastructure.EfCore;
using BuildingBlocks.Infrastructure.Secrets;
using Gloss.Api.Configs;
using Gloss.Api.Jobs;
using Gloss.Api.MergeRequests;
using Gloss.Api.Projection;
using Gloss.Api.Repositories;
using Gloss.Application.Configs;
using Gloss.Application.Jobs;
using Gloss.Application.MergeRequests;
using Gloss.Application.Projection;
using Gloss.Application.Repositories;
using Gloss.Application.Reviews;
using Gloss.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBuildingBlocksApi(builder.Configuration);
builder.Services.AddSecretEncryption(builder.Configuration);
builder.Services.AddConfigApplication();
builder.Services.AddRepositoryApplication();
builder.Services.AddMergeRequestApplication();
builder.Services.AddReviewApplication();
builder.Services.AddProjectionApplication();
builder.Services.AddJobApplication();
builder.Services.AddGlossInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IJobScheduler, HangfireJobScheduler>();
builder.Services.AddScoped<IRecurringJobRegistrar, PollJobRegistrar>();

var app = builder.Build();

await app.MigrateAsync<GlossDbContext>().ConfigureAwait(false);

app.UseBuildingBlocksApi(builder.Configuration);
app.MapBuildingBlocksDocumentation();
app.MapBuildingBlocksHealthChecks();
app.MapConfigEndpoints();
app.MapRepositoryEndpoints();
app.MapMergeRequestEndpoints();
app.MapDraftCommentEndpoints();
app.MapJobEndpoints();
app.MapProjectionEndpoints();

await app.RunAsync().ConfigureAwait(false);

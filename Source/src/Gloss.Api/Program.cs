using BuildingBlocks.Infrastructure.Api;
using BuildingBlocks.Infrastructure.Api.Documentation;
using BuildingBlocks.Infrastructure.Api.Health;
using BuildingBlocks.Infrastructure.EfCore;
using BuildingBlocks.Infrastructure.Secrets;
using Gloss.Api.Configs;
using Gloss.Application.Configs;
using Gloss.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBuildingBlocksApi(builder.Configuration);
builder.Services.AddSecretEncryption(builder.Configuration);
builder.Services.AddConfigApplication();
builder.Services.AddGlossInfrastructure(builder.Configuration);

var app = builder.Build();

await app.MigrateAsync<GlossDbContext>().ConfigureAwait(false);

app.UseBuildingBlocksApi(builder.Configuration);
app.MapBuildingBlocksDocumentation();
app.MapBuildingBlocksHealthChecks();
app.MapConfigEndpoints();

await app.RunAsync().ConfigureAwait(false);
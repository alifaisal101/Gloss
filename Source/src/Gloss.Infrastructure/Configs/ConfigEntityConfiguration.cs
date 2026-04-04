using BuildingBlocks.Domain.Models.Secrets;
using Gloss.Domain.Configs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gloss.Infrastructure.Configs;

internal sealed class ConfigEntityConfiguration : IEntityTypeConfiguration<Config>
{
    public void Configure(EntityTypeBuilder<Config> builder)
    {
        builder.ToTable("configs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GitProvider)
            .HasConversion(
                v => v.Value,
                v => GitProvider.Create(v).Value);

        builder.Property(x => x.GitBaseUrl)
            .HasConversion(
                v => v.AbsoluteUri,
                v => new Uri(v));

        builder.Property(x => x.GitToken)
            .HasConversion(
                v => v.CipherText,
                v => EncryptedSecret.FromCipherText(v));

        var listConverter = new ValueConverter<IReadOnlyList<string>, string[]>(
            v => v.ToArray(),
            v => v);

        var listComparer = new ValueComparer<IReadOnlyList<string>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode(StringComparison.Ordinal))),
            v => (IReadOnlyList<string>)v.ToArray());

        builder.Property(x => x.GitProjects)
            .HasConversion(listConverter, listComparer);

        builder.Property(x => x.LlmProvider)
            .HasConversion(
                v => v.Value,
                v => LlmProvider.Create(v).Value);

        builder.Property(x => x.LlmApiKey)
            .HasConversion(
                v => v.CipherText,
                v => EncryptedSecret.FromCipherText(v));
    }
}

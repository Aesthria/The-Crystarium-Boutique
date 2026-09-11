using System.Buffers.Binary;
using System.Text.Json;

namespace CrystariumBoutiquePlugin.Tests;

public sealed class DistributionIconTests
{
    private const string ExpectedIconUrl =
        "https://raw.githubusercontent.com/Aesthria/The-Crystarium-Boutique/main/src/CrystariumBoutique/images/icon.png";

    [Fact]
    public void RepositoryAndDistributedManifestsUseSamePublicIconUrl()
    {
        var root = FindRepositoryRoot();
        using var repositoryManifest = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root, "repo.json")));
        using var distributedManifest = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root, "src", "CrystariumBoutique", "CrystariumBoutique.json")));

        var repositoryIconUrl = repositoryManifest.RootElement[0]
            .GetProperty("IconUrl")
            .GetString();
        var distributedIconUrl = distributedManifest.RootElement
            .GetProperty("IconUrl")
            .GetString();

        Assert.Equal(ExpectedIconUrl, repositoryIconUrl);
        Assert.Equal(repositoryIconUrl, distributedIconUrl);
        Assert.True(Uri.TryCreate(distributedIconUrl, UriKind.Absolute, out var iconUri));
        Assert.Equal(Uri.UriSchemeHttps, iconUri!.Scheme);
        Assert.Equal("raw.githubusercontent.com", iconUri.Host);
        Assert.DoesNotContain("/releases/", iconUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/archive/", iconUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private", iconUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApprovedIconAssetIsValidSquarePng()
    {
        var iconPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CrystariumBoutique",
            "images",
            "icon.png");
        var png = File.ReadAllBytes(iconPath);

        Assert.True(png.Length >= 24);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png[..8]);
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(png, 12, 4));
        var width = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4));
        Assert.True(width > 0);
        Assert.Equal(width, height);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "repo.json"))
                && File.Exists(Path.Combine(
                    directory.FullName,
                    "src",
                    "CrystariumBoutique",
                    "CrystariumBoutique.json")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Crystarium Boutique repository root.");
    }
}

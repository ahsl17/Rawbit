using System;
using System.IO;
using System.Threading.Tasks;
using Rawbit.Services;
using Xunit;
using Xunit.Sdk;

namespace Rawbit.IntegrationTests.Loading;

public class RawLoaderIntegrationTests
{
    [Fact]
    public async Task LoadRawImageAsync_LoadsFullResAndProxy()
    {
        // GIVEN a real RAW file path (from assets or env)
        var path = ResolveRawPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw SkipException.ForSkip("Raw test file not found. Ensure Assets/Test.ARW is present or set RAWBIT_TEST_RAW_PATH.");
        }

        var service = new RawLoaderService();
        // WHEN loading the RAW file
        using var container = await service.LoadRawImageAsync(path);

        // THEN full-res and proxy images are produced with valid sizes
        Assert.NotNull(container.FullRes);
        Assert.NotNull(container.Proxy);
        Assert.True(container.Size.Width > 0);
        Assert.True(container.Size.Height > 0);
        Assert.True(container.Proxy!.Width <= 1024);
        Assert.True(container.Proxy!.Height <= 1024);
    }

    private static string ResolveRawPath()
    {
        var localPath = Path.Combine(AppContext.BaseDirectory, "Assets/Test.ARW");
        if (File.Exists(localPath))
        {
            return localPath;
        }

        return string.Empty;
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Rawbit.Graphics;
using Rawbit.Models;
using Rawbit.Services.Interfaces;
using SkiaSharp;

namespace Rawbit.Services;

public sealed class ExportService : IExportService
{
    private static Lock _renderLock = new();
    public Task ExportJpegAsync(
        RawImageContainer container,
        AdjustmentsState adjustments,
        string destinationFolder,
        string fileName,
        int quality,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var source = container.FullRes ?? container.Proxy;
                if (source == null)
                    return;
            
                var sourceInfo = new SKImageInfo(
                    source.Width,
                    source.Height,
                    source.ColorType,
                    source.AlphaType,
                    source.ColorSpace);
                using var bitmap = new SKBitmap(sourceInfo);
                if (!source.ReadPixels(sourceInfo, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
                    throw new InvalidOperationException("ReadPixels failed.");

                using var safeImage = SKImage.FromBitmap(bitmap);

                Directory.CreateDirectory(destinationFolder);
                var targetPath = Path.Combine(destinationFolder, fileName);

                using var engine = new RawRenderingEngine();
                var exportInfo = new SKImageInfo(
                    source.Width,
                    source.Height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Premul,
                    SKColorSpace.CreateSrgb());
                using var surface = SKSurface.Create(exportInfo)
                                    ?? throw new InvalidOperationException("Failed to create export surface.");

                var shader = ShaderSettings.From(adjustments);
                var render = new RenderSettings(1f, SKPoint.Empty, new SKRect(0, 0, safeImage.Width, safeImage.Height));

                lock (_renderLock)
                {
                    engine.Render(surface.Canvas, new RenderRequest(safeImage, shader, render));
                }

                using var image = surface.Snapshot();
                var clampedQuality = Math.Clamp(quality, 1, 100);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, clampedQuality);
                using var stream = File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                data.SaveTo(stream);

            }
            catch (Exception e)
            {
                // do nothing
            }
        }, cancellationToken);
    }
}

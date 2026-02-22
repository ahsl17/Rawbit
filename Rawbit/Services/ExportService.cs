using System;
using System.Collections.Generic;
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
    public Task ExportJpegAsync(
        SKImage source,
        AdjustmentsState adjustments,
        string destinationFolder,
        string fileName,
        int quality,
        CancellationToken cancellationToken,
        IProgress<double>? progress = null)
    {
        return Task.Run(
            () => ExportCpuTiled(source, adjustments, destinationFolder, fileName, quality, cancellationToken,
                progress), cancellationToken);
    }

    private void ExportCpuTiled(
        SKImage source,
        AdjustmentsState adjustments,
        string destinationFolder,
        string fileName,
        int quality,
        CancellationToken cancellationToken,
        IProgress<double>? progress)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(destinationFolder);
        var targetPath = Path.Combine(destinationFolder, fileName);

        var exportInfo = CreateExportInfo(source);
        var shader = ShaderSettings.From(adjustments);
        var clampedQuality = Math.Clamp(quality, 1, 100);

        var grid = TileGrid.Create(source.Width, source.Height, 512);
        var tiles = RenderTiles(source, shader, exportInfo, grid, cancellationToken, progress);

        using var exportSurface = ComposeTiles(exportInfo, grid, tiles);
        using var image = exportSurface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, clampedQuality);
        using var stream = File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        data.SaveTo(stream);
    }

    private static SKImageInfo CreateExportInfo(SKImage source) =>
        new(
            source.Width,
            source.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul,
            SKColorSpace.CreateSrgb());

    private static SKImage?[] RenderTiles(
        SKImage source,
        ShaderSettings shader,
        SKImageInfo exportInfo,
        TileGrid grid,
        CancellationToken cancellationToken,
        IProgress<double>? progress)
    {
        var tiles = new SKImage?[grid.TotalTiles];
        var completed = 0;

        Parallel.For(
            0,
            grid.TotalTiles,
            new ParallelOptions { CancellationToken = cancellationToken },
            () => new RawRenderingEngine(),
            (index, _, engine) =>
            {
                var tile = grid.GetTile(index);
                using var tileSurface = SKSurface.Create(tile.Info(exportInfo))
                                        ?? throw new InvalidOperationException("Failed to create tile surface.");

                var render = new RenderSettings(
                    1f,
                    new SKPoint(-tile.X, -tile.Y),
                    grid.FullRect);

                engine.Render(tileSurface.Canvas, new RenderRequest(source, shader, render));
                tiles[index] = tileSurface.Snapshot();

                var done = Interlocked.Increment(ref completed);
                progress?.Report((double)done / grid.TotalTiles);

                return engine;
            },
            engine => engine.Dispose());

        return tiles;
    }

    private static SKSurface ComposeTiles(SKImageInfo exportInfo, TileGrid grid, SKImage?[] tiles)
    {
        var exportSurface = SKSurface.Create(exportInfo)
                            ?? throw new InvalidOperationException("Failed to create export surface.");
        var canvas = exportSurface.Canvas;

        foreach (var tile in grid.EnumerateTiles())
        {
            var tileImage = tiles[tile.Index];
            if (tileImage == null)
                throw new InvalidOperationException("Tile render failed.");

            canvas.DrawImage(tileImage, tile.X, tile.Y);
            tileImage.Dispose();
        }

        return exportSurface;
    }

    private readonly record struct Tile(int Index, int X, int Y, int Width, int Height)
    {
        public SKImageInfo Info(SKImageInfo exportInfo) =>
            new(Width, Height, exportInfo.ColorType, exportInfo.AlphaType, exportInfo.ColorSpace);
    }

    private readonly record struct TileGrid(int Width, int Height, int TileSize, int TilesX, int TilesY)
    {
        public int TotalTiles => TilesX * TilesY;
        public SKRect FullRect => new(0, 0, Width, Height);

        public static TileGrid Create(int width, int height, int tileSize)
        {
            var tilesX = (width + tileSize - 1) / tileSize;
            var tilesY = (height + tileSize - 1) / tileSize;
            return new TileGrid(width, height, tileSize, tilesX, tilesY);
        }

        public Tile GetTile(int index)
        {
            var ty = index / TilesX;
            var tx = index % TilesX;
            var x = tx * TileSize;
            var y = ty * TileSize;
            var w = Math.Min(TileSize, Width - x);
            var h = Math.Min(TileSize, Height - y);
            return new Tile(index, x, y, w, h);
        }

        public IEnumerable<Tile> EnumerateTiles()
        {
            for (var i = 0; i < TotalTiles; i++)
                yield return GetTile(i);
        }
    }
}
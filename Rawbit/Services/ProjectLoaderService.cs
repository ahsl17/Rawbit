using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Rawbit.Data.Repositories.Interfaces;
using Rawbit.Helpers;
using Rawbit.Services.Interfaces;
using Sdcb.LibRaw;
using Sdcb.LibRaw.Natives;
using SkiaSharp;

namespace Rawbit.Services;


public record ThumnailWithPath(Bitmap Bitmap, string Path);

public class ProjectLoaderService : IProjectLoaderService
{
    private readonly IImageRepository _imageRepository;

    public ProjectLoaderService(IImageRepository imageRepository)
    {
        _imageRepository = imageRepository;
    }

    public List<ThumnailWithPath> LoadThumbnailsFromFolder(string folderPath)
    {
        var files = Directory.GetFiles(folderPath);
        var thumbnailsWithPaths = new List<ThumnailWithPath>();
        foreach (var filePath in files)
        {
            if (!FileHelper.RawSupportedFileFormats.Any(f => filePath.EndsWith(f, StringComparison.OrdinalIgnoreCase))) continue;
            using var context = RawContext.OpenFile(filePath);
            LibRawData data = Marshal.PtrToStructure<LibRawData>(context.UnsafeGetHandle());
            LibRawImageSizes sizes = data.ImageSizes;
            using ProcessedImage image = context.ExportThumbnail(thumbnailIndex: 0);
            using (var ms = new MemoryStream(image.AsSpan<byte>().ToArray()))
            {
                var bitmap = new Bitmap(ms);
                var oriented = ApplyLibRawOrientation(bitmap, sizes);
                thumbnailsWithPaths.Add(new ThumnailWithPath(oriented, filePath));
            }
        }

        return thumbnailsWithPaths;
    }

    public async Task RegisterImagesAsync(List<string> select)
    {
        var imagesToRegister = await _imageRepository
            .GetImagesToRegisterAsync(select)
            .ConfigureAwait(false);
        await _imageRepository.RegisterImagesAsync(imagesToRegister).ConfigureAwait(false);
    }

    static Bitmap ApplyLibRawOrientation(Bitmap src, LibRawImageSizes sizes)
    {
        double angle = sizes.Flip switch
        {
            3 => 180,
            5 => 270, // 90 CCW
            6 => 90,  // 90 CW
            _ => 0
        };

        if (angle == 0)
            return src;

        bool swap = angle is 90 or 270;
        var srcSize = src.Size;
        var dstSize = swap ? new Size(srcSize.Height, srcSize.Width) : srcSize;

        var rtb = new RenderTargetBitmap(new PixelSize((int)dstSize.Width, (int)dstSize.Height));

        using (var ctx = rtb.CreateDrawingContext(false))
        {
            var rad = Math.PI * angle / 180.0;

            // move origin to center, rotate, move back
            var centerSrc = new Point(srcSize.Width / 2, srcSize.Height / 2);
            var centerDst = new Point(dstSize.Width / 2, dstSize.Height / 2);

            var transform =
                Matrix.CreateTranslation(-centerSrc.X, -centerSrc.Y) *
                Matrix.CreateRotation(rad) *
                Matrix.CreateTranslation(centerDst.X, centerDst.Y);

            ctx.PushTransform(transform);
            ctx.DrawImage(src, new Rect(srcSize), new Rect(srcSize));
        }

        return rtb;
    }
}
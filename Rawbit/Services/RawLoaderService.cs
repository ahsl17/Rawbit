using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Rawbit.Models;
using Rawbit.Services.Interfaces;
using Sdcb.LibRaw;
using Sdcb.LibRaw.Natives;
using SkiaSharp;

namespace Rawbit.Services;

public class RawLoaderService : IRawLoaderService
{
    private const int ProxyLongSide = 1024; // Slightly larger for better preview quality
    private static readonly SKColorSpace ColorSpace = SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Linear, SKColorSpaceXyz.AdobeRgb);
    
    public async Task<RawImageContainer> LoadRawImageAsync(string path)
    {
        return await Task.Run(() =>
        {
            using var bitmap = DecodeToF16(path);
            
            // Generate images
            var fullResImage = SKImage.FromBitmap(bitmap);
            
            using var proxyBitmap = CreateProxy(bitmap);
            var proxyImage = SKImage.FromBitmap(proxyBitmap);

            return new RawImageContainer(
                fullResImage, 
                proxyImage, 
                new SKSize(bitmap.Width, 
                    bitmap.Height));
        }).ConfigureAwait(false);
    }

    private static SKBitmap DecodeToF16(string path)
    {
        using var context = RawContext.OpenFile(path);
        context.Unpack();
        
        context.DcrawProcess(cfg => 
        {
            cfg.OutputBps = 16;
            cfg.OutputColor = LibRawColorSpace.AdobeRgb; 
            cfg.UseCameraWb = true;
            cfg.NoAutoBright = true; 
            cfg.AutoScale = true;
            cfg.HighlightMode = 5;
            cfg.UserQual = DemosaicAlgorithm.AdaptiveHomogeneityDirected; 

            // Force absolute linear data
            cfg.Gamma[0] = 1.0; 
            cfg.Gamma[1] = 1.0;
        
            cfg.Threshold = 0.0f; 
        });
    
        using ProcessedImage image = context.MakeDcrawMemoryImage();

        // Use AdobeRgb linear space to match the LibRaw output
        var info = new SKImageInfo(
            image.Width, 
            image.Height, 
            SKColorType.RgbaF16, 
            SKAlphaType.Premul,
            ColorSpace
            ); 

        var bitmap = new SKBitmap(info);

        unsafe
        {
            ushort* src = (ushort*)image.DataPointer.ToPointer();
            Half* dst = (Half*)bitmap.GetPixels();
            int pixelCount = image.Width * image.Height;
            float invMax = 1.0f / 65535.0f; 
            int processorCount = Math.Max(1, Environment.ProcessorCount);
            int chunkSize = Math.Max(4096, pixelCount / (processorCount * 4));
            
            // Prevent missing pixel in case a chunk is not complete
            int ceilingIterations = (pixelCount + chunkSize - 1) / chunkSize;
            
            Parallel.For(0, ceilingIterations, chunk =>
            {
                int start = chunk * chunkSize;
                int endPixel = Math.Min(pixelCount, start + chunkSize);
                for (int i = start; i < endPixel; i++)
                {
                    dst[i * 4 + 0] = (Half)(src[i * 3 + 0] * invMax);
                    dst[i * 4 + 1] = (Half)(src[i * 3 + 1] * invMax);
                    dst[i * 4 + 2] = (Half)(src[i * 3 + 2] * invMax);
                    dst[i * 4 + 3] = (Half)1.0f;
                }
            });
        }
        return bitmap;
    }

    private static SKBitmap CreateProxy(SKBitmap full)
    {
        float ratio = (float)full.Width / full.Height;
        int w, h;

        if (full.Width > full.Height) {
            w = ProxyLongSide;
            h = (int)(ProxyLongSide / ratio);
        } else {
            h = ProxyLongSide;
            w = (int)(ProxyLongSide * ratio);
        }

        var info = new SKImageInfo(w, h, SKColorType.RgbaF16, SKAlphaType.Premul, ColorSpace);
        var proxy = new SKBitmap(info);

        // Linear filtering is important for avoiding moiré in proxies
        full.ScalePixels(proxy, new SKSamplingOptions(SKFilterMode.Linear));
        return proxy;
    }
}

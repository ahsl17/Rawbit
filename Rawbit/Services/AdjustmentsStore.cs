using System;
using System.Text.Json;
using System.Threading.Tasks;
using Rawbit.Data.Repositories.Interfaces;
using Rawbit.Models;
using Rawbit.Services.Interfaces;

namespace Rawbit.Services;

public sealed class AdjustmentsStore : IAdjustmentsStore
{
    private const int HslLength = 24;
    private const int CurveLength = 16;
    private readonly IImageRepository _imageRepository;

    public AdjustmentsStore(IImageRepository imageRepository)
    {
        _imageRepository = imageRepository;
    }

    public async Task<AdjustmentsState?> LoadAsync(string imagePath)
    {
        var adjustments = await _imageRepository.GetAdjustmentsByPathAsync(imagePath).ConfigureAwait(false);
        if (adjustments == null)
            return null;

        var hsl = DeserializeFloatArray(adjustments.HslAdjustmentsJson, HslLength);
        var curve = DeserializeFloatArray(adjustments.CurvePointsJson, CurveLength);

        return new AdjustmentsState(
            adjustments.Exposure,
            adjustments.Shadows,
            adjustments.Highlights,
            adjustments.Temperature,
            adjustments.Tint,
            hsl,
            curve,
            adjustments.CurvePointCount);
    }

    public async Task SaveAsync(string imagePath, AdjustmentsState state)
    {
        var adjustments = new Adjustments
        {
            Exposure = state.Exposure,
            Shadows = state.Shadows,
            Highlights = state.Highlights,
            Temperature = state.Temperature,
            Tint = state.Tint,
            HslAdjustmentsJson = SerializeFloatArray(state.Hsl),
            CurvePointsJson = SerializeFloatArray(state.CurvePoints),
            CurvePointCount = state.CurvePointCount
        };

        await _imageRepository.UpdateAdjustmentsByPathAsync(imagePath, adjustments).ConfigureAwait(false);
    }

    private static float[] DeserializeFloatArray(string json, int expectedLength)
    {
        try
        {
            var data = JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
            if (data.Length < expectedLength)
            {
                var padded = new float[expectedLength];
                Array.Copy(data, padded, data.Length);
                return padded;
            }
            return data;
        }
        catch
        {
            return new float[expectedLength];
        }
    }

    private static string SerializeFloatArray(float[] data)
    {
        return JsonSerializer.Serialize(data ?? Array.Empty<float>());
    }
}

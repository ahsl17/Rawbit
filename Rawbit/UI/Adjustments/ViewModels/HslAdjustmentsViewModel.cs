using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class HslAdjustmentsViewModel : ObservableObject
{
    private const int HslColorCount = 8;
    private readonly float[] _hslAdjustmentsPacked = new float[HslColorCount * 3];

    public event Action? AdjustmentsChanged;

    public ObservableCollection<HslColorAdjustmentViewModel> Adjustments { get; } = new();

    public float[] AdjustmentsPacked => _hslAdjustmentsPacked;

    public HslAdjustmentsViewModel()
    {
        Adjustments.CollectionChanged += OnHslAdjustmentsChanged;
        SeedHslAdjustments();
        UpdateHslCache();
    }

    private void OnHslAdjustmentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is HslColorAdjustmentViewModel hsl)
                    hsl.PropertyChanged -= OnHslAdjustmentPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is HslColorAdjustmentViewModel hsl)
                    hsl.PropertyChanged += OnHslAdjustmentPropertyChanged;
            }
        }

        UpdateHslCache();
        AdjustmentsChanged?.Invoke();
    }

    private void OnHslAdjustmentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateHslCache();
        AdjustmentsChanged?.Invoke();
    }

    private void SeedHslAdjustments()
    {
        if (Adjustments.Count > 0)
            return;

        Adjustments.Add(new HslColorAdjustmentViewModel("Red"));
        Adjustments.Add(new HslColorAdjustmentViewModel("Orange"));
        Adjustments.Add(new HslColorAdjustmentViewModel("Yellow"));
        Adjustments.Add(new HslColorAdjustmentViewModel("Green"));
        Adjustments.Add(new HslColorAdjustmentViewModel("Aqua"));
        Adjustments.Add(new HslColorAdjustmentViewModel("Blue"));
        Adjustments.Add(new HslColorAdjustmentViewModel("Purple"));
        Adjustments.Add(new HslColorAdjustmentViewModel("Magenta"));
    }

    private void UpdateHslCache()
    {
        for (int i = 0; i < _hslAdjustmentsPacked.Length; i++)
            _hslAdjustmentsPacked[i] = 0f;

        var count = Math.Min(Adjustments.Count, HslColorCount);
        for (int i = 0; i < count; i++)
        {
            var hsl = Adjustments[i];
            _hslAdjustmentsPacked[i * 3] = (float)hsl.Hue;
            _hslAdjustmentsPacked[i * 3 + 1] = (float)hsl.Saturation;
            _hslAdjustmentsPacked[i * 3 + 2] = (float)hsl.Luminance;
        }
    }
}

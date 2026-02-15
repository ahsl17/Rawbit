using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rawbit.UI.Adjustments.ViewModels;

public enum HslColors { Red, Orange, Yellow, Green, Aqua, Blue, Purple, Magenta }


public partial class HslAdjustmentsViewModel : ObservableObject
{
    private const int HslColorCount = 8;
    private readonly float[] _hslAdjustmentsPacked = new float[HslColorCount * 3];
    private static readonly IReadOnlyDictionary<HslColors, IBrush> HslSwatchBrushes =
        new Dictionary<HslColors, IBrush>
        {
            { HslColors.Red, Brushes.Red },
            { HslColors.Orange, Brushes.Orange },
            { HslColors.Yellow, Brushes.Yellow },
            { HslColors.Green, Brushes.Green },
            { HslColors.Aqua, Brushes.Aqua },
            { HslColors.Blue, Brushes.Blue },
            { HslColors.Purple, Brushes.Purple },
            { HslColors.Magenta, Brushes.Magenta }
        };

    public event Action? AdjustmentsChanged;

    public ObservableCollection<HslColorAdjustmentViewModel> AdjustmentsViewModels { get; } = new();

    [ObservableProperty] private HslColorAdjustmentViewModel _displayedHslColorAdjustment;
    
    public float[] AdjustmentsPacked => _hslAdjustmentsPacked;

    public HslAdjustmentsViewModel()
    {
        InitHslAdjustments();
        DisplayedHslColorAdjustment = AdjustmentsViewModels[0];
        DisplayedHslColorAdjustment.IsSelected = true;
        UpdateHslCache();
    }
    
    private void InitHslAdjustments()
    {
        if (AdjustmentsViewModels.Count > 0)
            return;

        foreach (var color in Enum.GetValues<HslColors>())
        {
            if (!HslSwatchBrushes.TryGetValue(color, out var brush))
                continue;

            AdjustmentsViewModels.Add(new HslColorAdjustmentViewModel(color, brush, OnAdjustmentsChanged, SelectAdjustment));
        }
    }

    public void OnAdjustmentsChanged()
    {
        UpdateHslCache();
        AdjustmentsChanged?.Invoke();

    }

    private void SelectAdjustment(HslColorAdjustmentViewModel adjustment)
    {
        if (DisplayedHslColorAdjustment == adjustment)
            return;

        DisplayedHslColorAdjustment = adjustment;
    }

    partial void OnDisplayedHslColorAdjustmentChanged(
        HslColorAdjustmentViewModel oldValue,
        HslColorAdjustmentViewModel newValue)
    {
        if (oldValue != null)
            oldValue.IsSelected = false;

        if (newValue != null)
            newValue.IsSelected = true;
    }
    
    private void UpdateHslCache()
    {
        for (int i = 0; i < _hslAdjustmentsPacked.Length; i++)
            _hslAdjustmentsPacked[i] = 0f;

        var count = Math.Min(AdjustmentsViewModels.Count, HslColorCount);
        for (int i = 0; i < count; i++)
        {
            var hsl = AdjustmentsViewModels[i];
            _hslAdjustmentsPacked[i * 3] = (float)hsl.Hue;
            _hslAdjustmentsPacked[i * 3 + 1] = (float)hsl.Saturation;
            _hslAdjustmentsPacked[i * 3 + 2] = (float)hsl.Luminance;
        }
    }
}

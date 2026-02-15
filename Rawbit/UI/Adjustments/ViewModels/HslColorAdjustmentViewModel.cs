using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class HslColorAdjustmentViewModel : ObservableObject
{
    private readonly Action _onAdjustmentsChanged;
    private readonly Action<HslColorAdjustmentViewModel> _onSelected;
    public HslColors Color { get; }
    public IBrush SwatchBrush { get; }

    [ObservableProperty] private double _hue;
    [ObservableProperty] private double _saturation;
    [ObservableProperty] private double _luminance;
    [ObservableProperty] private bool _isSelected;

    public HslColorAdjustmentViewModel(
        HslColors color,
        IBrush swatchBrush,
        Action onAdjustmentsChanged,
        Action<HslColorAdjustmentViewModel> onSelected)
    {
        _onAdjustmentsChanged = onAdjustmentsChanged;
        _onSelected = onSelected;
        Color = color;
        SwatchBrush = swatchBrush;
    }

    partial void OnHueChanged(double value)
    {
        _onAdjustmentsChanged.Invoke();
    }

    partial void OnLuminanceChanged(double value)
    {
        _onAdjustmentsChanged.Invoke();
    }

    partial void OnSaturationChanged(double value)
    {
        _onAdjustmentsChanged.Invoke();
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
            _onSelected.Invoke(this);
    }
}

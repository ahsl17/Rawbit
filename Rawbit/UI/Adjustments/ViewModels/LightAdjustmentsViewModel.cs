using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class LightAdjustmentsViewModel : ObservableObject
{
    public event Action? AdjustmentsChanged;

    [ObservableProperty] private double _exposureValue;
    [ObservableProperty] private double _shadowsValue;
    [ObservableProperty] private double _highlightsValue;
    [ObservableProperty] private double _temperatureValue;
    [ObservableProperty] private double _tintValue;

    partial void OnExposureValueChanged(double value) => AdjustmentsChanged?.Invoke();
    partial void OnShadowsValueChanged(double value) => AdjustmentsChanged?.Invoke();
    partial void OnHighlightsValueChanged(double value) => AdjustmentsChanged?.Invoke();
    partial void OnTemperatureValueChanged(double value) => AdjustmentsChanged?.Invoke();
    partial void OnTintValueChanged(double value) => AdjustmentsChanged?.Invoke();
}

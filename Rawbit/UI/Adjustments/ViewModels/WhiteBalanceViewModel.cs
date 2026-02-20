using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class WhiteBalanceViewModel : ObservableObject
{
    public event Action? AdjustmentsChanged;

    [ObservableProperty] private double _temperatureValue;
    [ObservableProperty] private double _tintValue;

    partial void OnTemperatureValueChanged(double value) => AdjustmentsChanged?.Invoke();
    partial void OnTintValueChanged(double value) => AdjustmentsChanged?.Invoke();
}

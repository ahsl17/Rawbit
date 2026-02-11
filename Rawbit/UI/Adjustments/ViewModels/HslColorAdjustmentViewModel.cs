using CommunityToolkit.Mvvm.ComponentModel;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class HslColorAdjustmentViewModel : ObservableObject
{
    public HslColorAdjustmentViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    [ObservableProperty] private double _hue;
    [ObservableProperty] private double _saturation;
    [ObservableProperty] private double _luminance;
}

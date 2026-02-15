using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class HslColorSelectorViewModel: ObservableObject
{
    [ObservableProperty] private Color _targetColor;
    
    private readonly Action<Color> _onColorSelected;
    
    [RelayCommand]
    public void SelectColor() => _onColorSelected?.Invoke(TargetColor);
    
    public HslColorSelectorViewModel(Color color, Action<Color> onColorSelected)
    {
        _targetColor = color;
        _onColorSelected = onColorSelected;
    }
    
}
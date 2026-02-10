using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rawbit.UI.ViewModels;

public class CurvePoint : INotifyPropertyChanged
{
    private double _x;
    private double _y;

    public CurvePoint(double x, double y)
    {
        _x = x;
        _y = y;
    }

    public double X
    {
        get => _x;
        set
        {
            if (value == _x)
                return;
            _x = value;
            OnPropertyChanged();
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (value == _y)
                return;
            _y = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

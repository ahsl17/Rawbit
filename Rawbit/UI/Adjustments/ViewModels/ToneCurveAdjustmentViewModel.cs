using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class ToneCurveAdjustmentViewModel : ObservableObject
{
    private const int MaxCurvePoints = 8;
    private readonly float[] _curvePointsPacked = new float[MaxCurvePoints * 2];
    private int _curvePointCount;

    public event Action? AdjustmentsChanged;

    public ObservableCollection<CurvePoint> Points { get; } = new();

    public float[] CurvePointsPacked => _curvePointsPacked;
    public int CurvePointCount => _curvePointCount;

    public ToneCurveAdjustmentViewModel()
    {
        Points.CollectionChanged += OnCurvePointsChanged;
        UpdateCurveCache();
    }

    private void OnCurvePointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is CurvePoint point)
                    point.PropertyChanged -= OnCurvePointPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is CurvePoint point)
                    point.PropertyChanged += OnCurvePointPropertyChanged;
            }
        }

        UpdateCurveCache();
        AdjustmentsChanged?.Invoke();
    }

    private void OnCurvePointPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateCurveCache();
        AdjustmentsChanged?.Invoke();
    }

    private void UpdateCurveCache()
    {
        _curvePointCount = Math.Min(Points.Count, MaxCurvePoints);

        var ordered = new List<CurvePoint>(Points);
        ordered.Sort((a, b) => a.X.CompareTo(b.X));

        for (int i = 0; i < _curvePointsPacked.Length; i++)
            _curvePointsPacked[i] = 0f;

        for (int i = 0; i < _curvePointCount; i++)
        {
            var p = ordered[i];
            _curvePointsPacked[i * 2] = (float)p.X;
            _curvePointsPacked[i * 2 + 1] = (float)p.Y;
        }
    }
}

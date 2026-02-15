using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rawbit.UI.Adjustments.ViewModels;

public class ToneCurveAdjustmentViewModel : ObservableObject
{
    private readonly float[] _curvePointsPacked = new float[8 * 2];
    private int _curvePointCount;
    private bool _suppressAdjustmentsChanged;

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
        if (!_suppressAdjustmentsChanged)
            AdjustmentsChanged?.Invoke();
    }

    private void OnCurvePointPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateCurveCache();
        if (!_suppressAdjustmentsChanged)
            AdjustmentsChanged?.Invoke();
    }

    private void UpdateCurveCache()
    {
        _curvePointCount = Points.Count;

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

    public void ApplyPacked(float[] packed, int count)
    {
        _suppressAdjustmentsChanged = true;
        try
        {
            Points.Clear();
            var max = Math.Min(count, packed.Length / 2);
            for (int i = 0; i < max; i++)
            {
                var x = packed[i * 2];
                var y = packed[i * 2 + 1];
                Points.Add(new CurvePoint(x, y));
            }
        }
        finally
        {
            _suppressAdjustmentsChanged = false;
        }

        UpdateCurveCache();
        AdjustmentsChanged?.Invoke();
    }
}
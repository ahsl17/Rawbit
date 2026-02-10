using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Rawbit.Models;
using Rawbit.Services;
using Rawbit.Services.Interfaces;
using Rawbit.UI.ViewModels.Interfaces;
using SkiaSharp;

namespace Rawbit.UI.ViewModels;

public partial class AdjustmentsViewModel : ObservableObject, INavigableViewModel
{
    private const int MaxCurvePoints = 8;
    private readonly object _syncLock = new();

    // Cached shader state
    private IRawLoaderService _rawLoaderService;
    private RawImageContainer? _rawImageContainer;
    [ObservableProperty] private List<ThumnailWithPath> _folderThumbnails = new();

    // UI state
    [ObservableProperty] private double _exposureValue;
    [ObservableProperty] private double _shadowsValue;
    [ObservableProperty] private double _highlightsValue;
    public ObservableCollection<CurvePoint> CurvePoints { get; } = new();
    private readonly float[] _curvePointsPacked = new float[MaxCurvePoints * 2];
    private int _curvePointCount;

    public float[] CurvePointsPacked => _curvePointsPacked;
    public int CurvePointCount => _curvePointCount;
    
    
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isRawLoaded;
    [ObservableProperty] private double _imageWidth;
    [ObservableProperty] private double _imageHeight;

    [ObservableProperty] private ThumnailWithPath? _selectedImage;

    private CancellationTokenSource? _loadCts;

    private SemaphoreSlim _loadGate = new(1, 1);
    private int _loadRequestId = 0;
    private int _redrawQueued;

    public Action? RequestRedraw { get; set; }

    private bool _isUserAdjusting;
    private CancellationTokenSource? _adjustmentCts;

    public AdjustmentsViewModel(IRawLoaderService rawLoaderService)
    {
        _rawLoaderService = rawLoaderService;

        CurvePoints.CollectionChanged += OnCurvePointsChanged;
        UpdateCurveCache();
    }

    partial void OnExposureValueChanged(double value) => AdjustAndRedraw();
    partial void OnShadowsValueChanged(double value) => AdjustAndRedraw();
    partial void OnHighlightsValueChanged(double value) => AdjustAndRedraw();

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
        AdjustAndRedraw();
    }

    private void OnCurvePointPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateCurveCache();
        AdjustAndRedraw();
    }

    private void UpdateCurveCache()
    {
        _curvePointCount = Math.Min(CurvePoints.Count, MaxCurvePoints);

        var ordered = new List<CurvePoint>(CurvePoints);
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

    partial void OnSelectedImageChanged(ThumnailWithPath? value)
    {
        if (value is null) return;
        _ = LoadImageAsync(value);
    }

    private void AdjustAndRedraw()
    {
        _isUserAdjusting = true;
        ResetAdjustmentTimer();
        RequestRedraw?.Invoke();
    }
    
    private async Task LoadImageAsync(ThumnailWithPath value)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var myReqId = Interlocked.Increment(ref _loadRequestId);
        Dispatcher.UIThread.Post(() => IsBusy = true);

        try
        {
            await _loadGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                
                RawImageContainer? oldContainer;
                Dispatcher.UIThread.Post(() => IsRawLoaded = false);
                lock (_syncLock)
                {
                    oldContainer = _rawImageContainer;
                    _rawImageContainer = null;
                }

                oldContainer?.Dispose();
                
                var newContainer = await _rawLoaderService.LoadRawImageAsync(value.Path).ConfigureAwait(false);

                if (token.IsCancellationRequested || myReqId != Volatile.Read(ref _loadRequestId))
                {
                    newContainer.Dispose();
                    return;
                }

                lock (_syncLock)
                {
                    _rawImageContainer = newContainer;
                }
                Dispatcher.UIThread.Post(() => IsRawLoaded = true);
            }
            catch
            {
                // Do Nothing
            }
            finally
            {
                _loadGate.Release();
            }

            Dispatcher.UIThread.Post(QueueRedraw);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception)
        {
            // Logger.Error(ex);
        }
        finally
        {
            // Only clear busy if we're still the latest request
            if (myReqId == Volatile.Read(ref _loadRequestId))
                Dispatcher.UIThread.Post(() => IsBusy = false);
        }
    }

    private void QueueRedraw()
    {
        if (Interlocked.Exchange(ref _redrawQueued, 1) == 1)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            IsBusy = false;
            Interlocked.Exchange(ref _redrawQueued, 0);
            RequestRedraw?.Invoke();
        }, DispatcherPriority.Render);
    }

    private void ResetAdjustmentTimer()
    {
        _adjustmentCts?.Cancel();
        _adjustmentCts = new CancellationTokenSource();
        var token = _adjustmentCts.Token;

        Task.Delay(150, token).ContinueWith(t =>
        {
            if (t.IsCanceled)
                return;

            _isUserAdjusting = false;
            Dispatcher.UIThread.Post(() => RequestRedraw?.Invoke());
        }, token);
    }

    public SKImage? ActiveImage
    {
        get
        {
            lock (_syncLock)
            {
                return (_isUserAdjusting && _rawImageContainer?.Proxy != null)
                    ? _rawImageContainer.Proxy
                    : _rawImageContainer?.FullRes;
            }
        }
    }

    public RawImageContainer? CurrentContainer
    {
        get
        {
            lock (_syncLock)
            {
                return _rawImageContainer;
            }
        }
    }

    public SKImage? ProxyImage
    {
        get
        {
            lock (_syncLock)
            {
                return _rawImageContainer?.Proxy;
            }
        }
    }
}

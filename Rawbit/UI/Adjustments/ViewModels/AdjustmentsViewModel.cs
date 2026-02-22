using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Rawbit.Models;
using Rawbit.Services;
using Rawbit.Services.Interfaces;
using Rawbit.UI.Adjustments.Interfaces;
using Rawbit.UI.Root.Interfaces;
using SkiaSharp;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class AdjustmentsViewModel : ObservableObject, INavigableViewModel, IAdjustmentsSnapshotProvider
{
    private readonly Lock _syncLock = new();

    // Cached shader state
    private readonly IRawLoaderService _rawLoaderService;
    private readonly IAdjustmentsStore _adjustmentsStore;
    private RawImageContainer? _rawImageContainer;
    [ObservableProperty] private List<ThumnailWithPath> _folderThumbnails = [];

    // UI state
    public LightAdjustmentsViewModel LightAdjustments { get; }
    public WhiteBalanceViewModel WhiteBalance { get; }
    public HslAdjustmentsViewModel HslAdjustments { get; }
    public ToneCurveAdjustmentViewModel ToneCurveAdjustment { get; }


    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isRawLoaded;
    [ObservableProperty] private double _imageWidth;
    [ObservableProperty] private double _imageHeight;

    [ObservableProperty] private ThumnailWithPath? _selectedImage;

    private CancellationTokenSource? _loadCts;

    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private int _loadRequestId = 0;
    private int _redrawQueued;

    public Action? RequestRedraw { get; set; }

    private bool _isUserAdjusting;
    private bool _isApplyingAdjustments;
    private CancellationTokenSource? _adjustmentCts;
    private CancellationTokenSource? _saveCts;

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

    public ExportViewModel Export { get; }

    public AdjustmentsViewModel(
        IRawLoaderService rawLoaderService,
        IAdjustmentsStore adjustmentsStore,
        IExportService exportService)
    {
        _rawLoaderService = rawLoaderService;
        _adjustmentsStore = adjustmentsStore;
        LightAdjustments = new LightAdjustmentsViewModel();
        WhiteBalance = new WhiteBalanceViewModel();
        HslAdjustments = new HslAdjustmentsViewModel();
        ToneCurveAdjustment = new ToneCurveAdjustmentViewModel();
        LightAdjustments.AdjustmentsChanged += AdjustAndRedraw;
        WhiteBalance.AdjustmentsChanged += AdjustAndRedraw;
        HslAdjustments.AdjustmentsChanged += AdjustAndRedraw;
        ToneCurveAdjustment.AdjustmentsChanged += AdjustAndRedraw;
        Export = new ExportViewModel(exportService, this);
    }

    partial void OnSelectedImageChanged(ThumnailWithPath? value)
    {
        if (value is null)
            return;
        if (!ReferenceEquals(SelectedImage, value))
            SelectedImage = value;

        _ = LoadAdjustmentsAsync(value.Path);
        _ = LoadImageAsync(value);
    }

    private async Task LoadAdjustmentsAsync(string imagePath)
    {
        try
        {
            var state = await _adjustmentsStore.LoadAsync(imagePath).ConfigureAwait(false);
            if (state == null)
                return;

            Dispatcher.UIThread.Post(() => ApplyAdjustments(state));
        }
        catch
        {
            // ignore
        }
    }

    private void AdjustAndRedraw()
    {
        if (_isApplyingAdjustments)
            return;
        _isUserAdjusting = true;
        ResetAdjustmentTimer();
        QueueSaveAdjustments();
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

    private void ApplyAdjustments(AdjustmentsState adjustments)
    {
        _isApplyingAdjustments = true;
        try
        {
            LightAdjustments.ExposureValue = adjustments.Exposure;
            LightAdjustments.ShadowsValue = adjustments.Shadows;
            LightAdjustments.HighlightsValue = Math.Clamp(adjustments.Highlights, -1f, 1f);
            LightAdjustments.WhitesValue = Math.Clamp(adjustments.Whites, -1f, 1f);
            LightAdjustments.BlacksValue = Math.Clamp(adjustments.Blacks, -1f, 1f);
            WhiteBalance.TemperatureValue = adjustments.Temperature;
            WhiteBalance.TintValue = adjustments.Tint;

            HslAdjustments.ApplyPacked(adjustments.Hsl);
            ToneCurveAdjustment.ApplyPacked(adjustments.CurvePoints, adjustments.CurvePointCount);
        }
        finally
        {
            _isApplyingAdjustments = false;
        }
    }

    private void QueueSaveAdjustments()
    {
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        Task.Delay(250, token).ContinueWith(async t =>
        {
            if (t.IsCanceled)
                return;
            await SaveCurrentAdjustmentsAsync().ConfigureAwait(false);
        }, token);
    }

    private async Task SaveCurrentAdjustmentsAsync()
    {
        var imagePath = SelectedImage?.Path;
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        var state = GetCurrentAdjustmentsState(snapshotArrays: true);

        await _adjustmentsStore.SaveAsync(imagePath, state).ConfigureAwait(false);
    }

    public AdjustmentsState GetCurrentAdjustmentsStateSnapshot() =>
        GetCurrentAdjustmentsState(snapshotArrays: true);

    public RawImageContainer? GetCurrentImageContainer()
    {
        lock (_syncLock)
        {
            return _rawImageContainer;
        }
    }

    public string? GetSelectedImagePath() => SelectedImage?.Path;
    public SKImage? GetActiveImage() => ActiveImage;

    public SKImage? GetFullResImage()
    {
        lock (_syncLock)
        {
            var source = _rawImageContainer?.FullRes;
            if (source == null)
                throw new NullReferenceException("FullRes image is null");

            var info = new SKImageInfo(source.Width, source.Height, source.ColorType, source.AlphaType,
                source.ColorSpace);
            using var bitmap = new SKBitmap(info);

            if (!source.ReadPixels(info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
                return null;

            return SKImage.FromBitmap(bitmap);
        }
    }

    public AdjustmentsState GetCurrentAdjustmentsState(bool snapshotArrays)
    {
        var hsl = snapshotArrays
            ? HslAdjustments.AdjustmentsPacked.ToArray()
            : HslAdjustments.AdjustmentsPacked;

        var curve = snapshotArrays
            ? ToneCurveAdjustment.CurvePointsPacked.ToArray()
            : ToneCurveAdjustment.CurvePointsPacked;

        return new AdjustmentsState(
            (float)LightAdjustments.ExposureValue,
            (float)LightAdjustments.ShadowsValue,
            (float)LightAdjustments.HighlightsValue,
            (float)LightAdjustments.WhitesValue,
            (float)LightAdjustments.BlacksValue,
            (float)WhiteBalance.TemperatureValue,
            (float)WhiteBalance.TintValue,
            hsl,
            curve,
            ToneCurveAdjustment.CurvePointCount);
    }
}
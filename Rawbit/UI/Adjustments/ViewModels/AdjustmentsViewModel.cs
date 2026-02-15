using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Rawbit.Models;
using Rawbit.Services;
using Rawbit.Services.Interfaces;
using Rawbit.UI.Root.Interfaces;
using SkiaSharp;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class AdjustmentsViewModel : ObservableObject, INavigableViewModel
{
    private readonly Lock _syncLock = new();

    // Cached shader state
    private readonly IRawLoaderService _rawLoaderService;
    private RawImageContainer? _rawImageContainer;
    [ObservableProperty] private List<ThumnailWithPath> _folderThumbnails = [];

    // UI state
    public LightAdjustmentsViewModel LightAdjustments { get; }
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
    private CancellationTokenSource? _adjustmentCts;

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
    
    public AdjustmentsViewModel(IRawLoaderService rawLoaderService)
    {
        _rawLoaderService = rawLoaderService;
        LightAdjustments = new LightAdjustmentsViewModel();
        HslAdjustments = new HslAdjustmentsViewModel();
        ToneCurveAdjustment = new ToneCurveAdjustmentViewModel();
        LightAdjustments.AdjustmentsChanged += AdjustAndRedraw;
        HslAdjustments.AdjustmentsChanged += AdjustAndRedraw;
        ToneCurveAdjustment.AdjustmentsChanged += AdjustAndRedraw;
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
}

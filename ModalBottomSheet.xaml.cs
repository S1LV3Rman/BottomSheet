#pragma warning disable CS0169, CS4014

using Maui.BindableProperty.Generator.Core;
using Microsoft.Maui.Controls.Shapes;

namespace S1LV3Rman.BottomSheet;

public partial class ModalBottomSheet : ContentView
{
    internal enum BSState
    {
        Drag,
        Edge,
        Scroll
    }

    [AutoBindable(DefaultValue = "20d", OnChanged = nameof(OnCornersRadiusChanged))]
    private double _cornersRadius;

    [AutoBindable(DefaultValue = "Color.FromArgb(\"#ffffffff\")")]
    private Color _color;

    [AutoBindable(HidesUnderlyingProperty = true)]
    private Thickness _padding;

    [AutoBindable(DefaultValue = "450u")]
    private uint _showDuration;
    
    [AutoBindable(DefaultValue = "350u")]
    private uint _hideDuration;

    [AutoBindable(DefaultValue = "Easing.CubicOut")]
    private Easing _showEasing;

    [AutoBindable(DefaultValue = "Easing.CubicInOut")]
    private Easing _hideEasing;

    [AutoBindable(DefaultValue = "true", DefaultBindingMode = nameof(BindingMode.OneWayToSource))]
    private bool _isOpened;

    [AutoBindable(DefaultValue = "false", OnChanged = nameof(OnBlockExpandingChanged))]
    private bool _blockExpanding;
    
    private RoundRectangle _tint;
    private RoundRectangle _bodyBacking;
    private Grid _body;
    private RoundRectangle _background;
    private ScrollView _scroll;
    private ContentPresenter _content;

    private DateTime _panStartTime;
    
    private bool IsMoving => _currentBodyAnimation != null && (int)_currentBodyAnimation.Status < 5;
    private Task _currentBodyAnimation;
    
    private bool IsFading => _currentTintAnimation != null && (int)_currentTintAnimation.Status < 5;
    private Task _currentTintAnimation;

    private double _currentGestureVelocity;
    
    private double _pageHeight;
    private double PageHeight
    {
        get => _pageHeight;
        set
        {
            if (_pageHeight != value)
            {
                _pageHeight = value;
                OnPageHeightChanged?.Invoke();
            }
        }
    }

    private event Action OnPageHeightChanged;
    
    private double _contentHeight;
    private double ContentHeight
    {
        get => _contentHeight;
        set
        {
            if (_contentHeight != value)
            {
                _contentHeight = value;
                OnContentHeightChanged?.Invoke();
            }
        }
    }

    private event Action OnContentHeightChanged;

    private double _startY;
    private double _startScrollY;

    private double _startDeltaY;    

    private BSState _currentState = BSState.Edge;

    private double CurrentY
    {
        get => _body.TranslationY;
        set => _body.TranslationY = value;
    }
    private double MinimizedHeight => Math.Min(_pageHeight * MAX_MINIMIZED_SIZE, ContentHeight + Padding.VerticalThickness);
    private double MinY => BlockExpanding ? _pageHeight - MinimizedHeight : 0d;

    private const double SWIPE_MIN_VELOCITY = 0.7d;
    private const double SWIPE_MIN_LENGTH = 75d;

    private const double THRESHOLD_DELTA = 40d;
    private const double MAX_MINIMIZED_SIZE = 0.5d;

	public ModalBottomSheet()
	{
		InitializeComponent();
        
        OnContentHeightChanged += UpdateScrollHeight;
        OnPageHeightChanged += UpdateScrollHeight;

        OnContentHeightChanged += Init;
        OnPageHeightChanged += Init;
    }

    private void Init()
    {
        if (PageHeight <= 0 || ContentHeight <= 0)
            return;

        MoveToAsync(PageHeight, 0u, Easing.Linear);
        FadeAsync(0u);
        IsOpened = false;

        OnPageHeightChanged -= Init;
    }

    //
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tint = (RoundRectangle)GetTemplateChild("tint");
        _bodyBacking = (RoundRectangle)GetTemplateChild("bodyBacking");
        _body = (Grid)GetTemplateChild("body");
        _background = (RoundRectangle)GetTemplateChild("background");
        _content = (ContentPresenter)GetTemplateChild("content");
        _scroll = (ScrollView)GetTemplateChild("scroll");

        ContentHeight = _content.Height;
        _scroll.IsEnabled = false;

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        _bodyBacking.GestureRecognizers.Add(pan);

        var bodyTap = new TapGestureRecognizer();
        bodyTap.Tapped += BodyTapped;
        bodyTap.NumberOfTapsRequired = 1;
        _bodyBacking.GestureRecognizers.Add(bodyTap);

        var tintTap = new TapGestureRecognizer();
        tintTap.Tapped += TintTapped;
        tintTap.NumberOfTapsRequired = 1;
        _tint.GestureRecognizers.Add(tintTap);
    }

    private void CancelMoving()
    {
        _body.CancelAnimations();
        _bodyBacking.CancelAnimations();
        _currentBodyAnimation = null;
    }

    private void TintTapped(object sender, EventArgs e)
    {
        HideAsync();
    }

    private void BodyTapped(object sender, EventArgs e)
    {
        if (!BlockExpanding)
            ExpandAsync();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        
        UpdateBackground();
        UpdateScrollHeight();
    }

    protected override Size ArrangeOverride(Rect bounds)
    {
        var parent = this.Parent as VisualElement;
        PageHeight = parent.Height;
        ContentHeight = _content.Height;

        return base.ArrangeOverride(bounds);
    }

    private void OnCornersRadiusChanged()
    {
        UpdateBackground();
    }

    private void OnBlockExpandingChanged()
    {
        UpdateBackground();
    }

    private void UpdateBackground()
    {        
        _background.CornerRadius = new CornerRadius(CornersRadius, CornersRadius, 0d, 0d);
    }

    private void UpdateScrollHeight()
    {   
        if (PageHeight <= 0d || ContentHeight <= 0d)
            return;

        if (BlockExpanding)
            _scroll.HeightRequest = MinimizedHeight;
        else
            _scroll.HeightRequest = PageHeight;
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (IsMoving)
            CancelMoving();

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartTime = DateTime.Now;

                _startY = CurrentY;
                _startScrollY = _scroll.ScrollY;

                _startDeltaY = 0d;
                break;

            case GestureStatus.Running:

                switch (_currentState)
                {
                    case BSState.Drag:
                        CurrentY = Math.Clamp(_startY + e.TotalY - _startDeltaY, MinY, PageHeight);
                        if (CurrentY <= MinY) // From drag to edge
                        {
                            _startDeltaY = e.TotalY;
                            _currentState = BSState.Edge;
                        }
                        break;

                    case BSState.Edge:
                        if (_startDeltaY - e.TotalY >= 0) // From edge to scroll
                        {
                            _startScrollY = _scroll.ScrollY;
                            _currentState = BSState.Scroll;
                            _scroll.ScrollToAsync(0d, _startScrollY + _startDeltaY - e.TotalY, false);
                        }
                        else // from edge to drag
                        {
                            _startY = CurrentY;    
                            _currentState = BSState.Drag;                        
                            CurrentY = Math.Clamp(_startY + e.TotalY - _startDeltaY, MinY, PageHeight);
                        }
                        break;

                    case BSState.Scroll:
                        _scroll.ScrollToAsync(0d, _startScrollY + _startDeltaY - e.TotalY, false);
                        if (_scroll.ScrollY <= 0d) // From scroll to edge
                        {
                            _startDeltaY = e.TotalY;
                            _currentState = BSState.Edge;
                        }
                        break;
                }
                break;

            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                _bodyBacking.TranslationY = CurrentY;

                var length = _startY - CurrentY;
                var duration = DateTime.Now - _panStartTime;
                _currentGestureVelocity = length / duration.TotalMilliseconds;

                if (Math.Abs(_currentGestureVelocity) > SWIPE_MIN_VELOCITY &&
                    Math.Abs(length) > SWIPE_MIN_LENGTH)
                {
                    // Swipe
                    if (length > 0) // Up
                        ExpandAsync();
                    else // Down
                        CollapseAsync();
                }
                else
                {
                    // Pan
                    var threshold = PageHeight - MinimizedHeight;

                    if (CurrentY > threshold + THRESHOLD_DELTA)
                        CollapseAsync();
                    else if (CurrentY < THRESHOLD_DELTA ||
                        CurrentY < threshold - THRESHOLD_DELTA && _startY > CurrentY)
                        ExpandAsync();
                    else
                        MinimizeAsync();
                }

                _currentGestureVelocity = 0d;
                break;
        }
    }

    private async Task MoveToAsync(double destinationY, uint duration, Easing easing)
    {
        if (duration < 0)
            throw new ArgumentOutOfRangeException(nameof(duration));

        if (IsMoving)
            return;

        _currentBodyAnimation = Task.WhenAll(
            _body.TranslateTo(0, destinationY, duration, easing),
            _bodyBacking.TranslateTo(0, destinationY, duration, easing));

        await _currentBodyAnimation;
    }

    private void AdjustDurationByVelocity(ref uint duration, double velocity, double length)
    {
        if (velocity < 0)
            throw new ArgumentOutOfRangeException(nameof(velocity));

        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (velocity > 0 &&
            velocity * duration * 0.001d > length)
        {
            duration = (uint) Math.Round(length / velocity * 1000d);
        }
    }

    private async Task ExpandAsync()
    {
        var animationDuration = (uint) Math.Round(CurrentY / PageHeight * 1000d);

        AdjustDurationByVelocity(ref animationDuration, Math.Abs(_currentGestureVelocity), CurrentY);

        await MoveToAsync(0, animationDuration, Easing.CubicOut);
    }

    private async Task MinimizeAsync()
    {
        var minimizedHeight = MinimizedHeight;
        var minimizedY = PageHeight - minimizedHeight;
        var animationDuration = CurrentY > minimizedY ?
            (uint) Math.Round((CurrentY - minimizedY) / minimizedHeight * 1000d) :
            (uint) Math.Round((minimizedY - CurrentY) / (PageHeight - minimizedHeight) * 1000d);

        AdjustDurationByVelocity(ref animationDuration, Math.Abs(_currentGestureVelocity), Math.Abs(CurrentY - minimizedY));

        await MoveToAsync(minimizedY, animationDuration, Easing.CubicOut);
    }

    private async Task CollapseAsync()
    {
        var animationDuration = (uint) Math.Round((PageHeight - CurrentY) / PageHeight * 1000d);

        AdjustDurationByVelocity(ref animationDuration, Math.Abs(_currentGestureVelocity), Math.Abs(CurrentY - PageHeight));

        await Task.WhenAll(
            MoveToAsync(PageHeight, animationDuration, Easing.CubicOut),
            FadeAsync(animationDuration));
        
        if (_currentBodyAnimation != null)
            IsOpened = false;
    }

    private async Task FadeAsync(uint duration)
    {
        if (IsFading)
        {
            _tint.CancelAnimations();
            _currentTintAnimation = null;
        }

        _currentTintAnimation = _tint.FadeTo(0d, duration, Easing.SinOut);
        await _currentTintAnimation;
    }

    private async Task BrightenAsync(uint duration)
    {
        if (IsFading)
        {
            _tint.CancelAnimations();
            _currentTintAnimation = null;
        }

        _currentTintAnimation = _tint.FadeTo(1d, duration, Easing.SinOut);
        await _currentTintAnimation;
    }

    public async Task ShowAsync()
    {
        _scroll.ScrollToAsync(0d, 0d, false);
        _currentState = BSState.Drag;

        IsOpened = true;
        await Task.WhenAll(
            MoveToAsync(PageHeight - MinimizedHeight, ShowDuration, ShowEasing),
            BrightenAsync(ShowDuration));
    }

    public async Task HideAsync()
    {
        if (IsMoving)
            CancelMoving();

        await Task.WhenAll(
            MoveToAsync(PageHeight, HideDuration, HideEasing),
            FadeAsync(HideDuration));

        if (_currentBodyAnimation != null)
            IsOpened = false;
    }

    public async Task ToggleAsync()
    {
        if (IsMoving)
            CancelMoving();

        if (IsOpened)
            await HideAsync();
        else
            await ShowAsync();
    }
}
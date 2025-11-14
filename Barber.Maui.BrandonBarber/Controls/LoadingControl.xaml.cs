namespace Barber.Maui.BrandonBarber.Controls
{
    public partial class LoadingControl : ContentView
    {
        public static readonly BindableProperty IsLoadingProperty =
            BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(LoadingControl), false,
                BindingMode.TwoWay, propertyChanged: OnIsLoadingChanged);

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        private bool _isAnimating;

        public LoadingControl()
        {
            InitializeComponent();
        }

        private static void OnIsLoadingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (LoadingControl)bindable;
            bool isLoading = (bool)newValue;

            if (isLoading)
                control.StartAnimation();
            else
                control.StopAnimation();
        }

        private void StartAnimation()
        {
            _isAnimating = true;
            _ = AnimateLogo();
            _ = AnimateText();
        }

        private void StopAnimation()
        {
            _isAnimating = false;
            this.IsVisible = false;
        }

        private async Task AnimateLogo()
        {
            while (_isAnimating)
            {
                await LogoImage.FadeTo(0.3, 800, Easing.CubicInOut);
                await LogoImage.FadeTo(1, 800, Easing.CubicInOut);
            }
        }

        private async Task AnimateText()
        {
            while (_isAnimating)
            {
                await LoadingLabel.FadeTo(0.3, 800, Easing.CubicInOut);
                await LoadingLabel.FadeTo(1, 800, Easing.CubicInOut);
            }
        }
    }
}

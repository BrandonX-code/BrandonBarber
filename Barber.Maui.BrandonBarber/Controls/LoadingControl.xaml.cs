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
            this.IsVisible = true;

            // Animación Logo
            var logoAnimation = new Animation(v =>
            {
                LogoImage.Opacity = v;
            }, 0.3, 1);
            logoAnimation.Commit(this, "LogoAnimation", length: 1600, easing: Easing.CubicInOut, repeat: () => true);

            // Animación Texto
            var textAnimation = new Animation(v =>
            {
                LoadingLabel.Opacity = v;
            }, 0.3, 1);
            textAnimation.Commit(this, "TextAnimation", length: 1600, easing: Easing.CubicInOut, repeat: () => true);
        }


        private void StopAnimation()
        {
            this.IsVisible = false;

            this.AbortAnimation("LogoAnimation");
            this.AbortAnimation("TextAnimation");
        }
    }
}

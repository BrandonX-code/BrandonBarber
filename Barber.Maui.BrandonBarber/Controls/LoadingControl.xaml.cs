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
            this.InputTransparent = false;

            // Deshabilitar el contenido padre
            if (this.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child != this && child is View view)
                    {
                        view.IsEnabled = false;
                    }
                }
            }

            // Animaciones...
            var logoAnimation = new Animation(v => { LogoImage.Opacity = v; }, 0.3, 1);
            logoAnimation.Commit(this, "LogoAnimation", length: 1600, easing: Easing.CubicInOut, repeat: () => true);

            var textAnimation = new Animation(v => { LoadingLabel.Opacity = v; }, 0.3, 1);
            textAnimation.Commit(this, "TextAnimation", length: 1600, easing: Easing.CubicInOut, repeat: () => true);
        }

        private void StopAnimation()
        {
            // Rehabilitar el contenido padre
            if (this.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child != this && child is View view)
                    {
                        view.IsEnabled = true;
                    }
                }
            }

            this.IsVisible = false;
            this.InputTransparent = true;
            this.AbortAnimation("LogoAnimation");
            this.AbortAnimation("TextAnimation");
        }
    }
}

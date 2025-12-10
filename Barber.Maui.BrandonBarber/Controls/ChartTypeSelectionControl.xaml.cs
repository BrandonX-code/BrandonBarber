namespace Barber.Maui.BrandonBarber.Controls
{
    public partial class ChartTypeSelectionControl : ContentView
    {
        public static readonly BindableProperty SelectedIndexProperty =
          BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(ChartTypeSelectionControl), 0, propertyChanged: OnSelectedIndexChanged);

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public event EventHandler<int>? SelectionChanged;

        public ChartTypeSelectionControl()
        {
            InitializeComponent();
            UpdateSelection(SelectedIndex);
            SetupTapGestures();
        }

        private void SetupTapGestures()
        {
            var barrasTap = new TapGestureRecognizer();
            barrasTap.Tapped += (s, e) =>
                   {
                       SelectedIndex = 0;
                       SelectionChanged?.Invoke(this, 0);
                   };
            var barrasButton = this.FindByName<Border>("BarrasButton");
            if (barrasButton != null)
                barrasButton.GestureRecognizers.Add(barrasTap);

            var lineasTap = new TapGestureRecognizer();
            lineasTap.Tapped += (s, e) =>
                    {
                        SelectedIndex = 1;
                        SelectionChanged?.Invoke(this, 1);
                    };
            var lineasButton = this.FindByName<Border>("LineasButton");
            if (lineasButton != null)
                lineasButton.GestureRecognizers.Add(lineasTap);
        }

        private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ChartTypeSelectionControl)bindable;
            if (newValue is int index)
            {
                control.UpdateSelection(index);
            }
        }

        private void UpdateSelection(int selectedIndex)
        {
            var barrasButton = this.FindByName<Border>("BarrasButton");
            var lineasButton = this.FindByName<Border>("LineasButton");

            var barrasIconWhite = this.FindByName<Image>("BarrasIconWhite");
            var barrasIconBlack = this.FindByName<Image>("BarrasIconBlack");

            var lineasIconWhite = this.FindByName<Image>("LineasIconWhite");
            var lineasIconBlack = this.FindByName<Image>("LineasIconBlack");

            var barrasGrid = barrasButton?.Content as Grid;
            var lineasGrid = lineasButton?.Content as Grid;

            var barrasLabel = barrasGrid?.Children[2] as Label;
            var lineasLabel = lineasGrid?.Children[2] as Label;

            // BARRAS
            if (barrasButton != null)
            {
                bool selected = selectedIndex == 0;

                barrasButton.BackgroundColor = selected ? Color.FromArgb("#0E2A36") : Color.FromArgb("#90A4AE");
                barrasButton.Stroke = selected ? Color.FromArgb("#0E2A36") : Color.FromArgb("#707070");

                barrasIconWhite.IsVisible = selected;
                barrasIconBlack.IsVisible = !selected;

                if (barrasLabel != null)
                    barrasLabel.TextColor = selected ? Colors.White : Colors.Black;
            }

            // LÍNEAS
            if (lineasButton != null)
            {
                bool selected = selectedIndex == 1;

                lineasButton.BackgroundColor = selected ? Color.FromArgb("#0E2A36") : Color.FromArgb("#90A4AE");
                lineasButton.Stroke = selected ? Color.FromArgb("#0E2A36") : Color.FromArgb("#707070");

                lineasIconWhite.IsVisible = selected;
                lineasIconBlack.IsVisible = !selected;

                if (lineasLabel != null)
                    lineasLabel.TextColor = selected ? Colors.White : Colors.Black;
            }
        }


    }
}

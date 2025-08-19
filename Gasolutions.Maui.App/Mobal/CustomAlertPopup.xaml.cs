using CommunityToolkit.Maui.Views;

namespace Gasolutions.Maui.App.Mobal
{
    public partial class CustomAlertPopup : Popup
    {
        private TaskCompletionSource<bool> _tcs;

        public CustomAlertPopup(string message)
        {
            InitializeComponent();
            MessageLabel.Text = message;

            YesButton.Clicked += (_, __) => { Close(true); };
            NoButton.Clicked += (_, __) => { Close(false); };
        }

        public Task<bool> ShowAsync(Page page)
        {
            _tcs = new TaskCompletionSource<bool>();

            this.Closed += (_, e) =>
            {
                if (e.Result is bool result)
                    _tcs.TrySetResult(result);
                else
                    _tcs.TrySetResult(false); // fallback
            };

            page.ShowPopup(this);
            return _tcs.Task;
        }
    }

}

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

namespace Gasolutions.Maui.App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private float _downX, _downY;
        private const int TapThreshold = 20; // píxeles permitidos para considerar tap

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.SetSoftInputMode(Android.Views.SoftInput.AdjustResize);
        }

        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    _downX = ev.RawX;
                    _downY = ev.RawY;
                    break;
                case MotionEventActions.Up:
                    float upX = ev.RawX;
                    float upY = ev.RawY;
                    float deltaX = Math.Abs(upX - _downX);
                    float deltaY = Math.Abs(upY - _downY);
                    if (deltaX < TapThreshold && deltaY < TapThreshold)
                    {
                        var focusedView = CurrentFocus;
                        if (focusedView is Android.Views.View currentView)
                        {
                            var rootView = Window.DecorView.RootView;
                            var touchedView = FindTouchedEditText(rootView, upX, upY);
                            if (touchedView != null && touchedView != currentView)
                            {
                                return base.DispatchTouchEvent(ev);
                            }
                            if (touchedView == null)
                            {
                                var imm = GetSystemService(InputMethodService) as InputMethodManager;
                                imm?.HideSoftInputFromWindow(currentView.WindowToken, HideSoftInputFlags.None);
                                currentView.ClearFocus();
                            }
                        }
                    }
                    break;
            }
            return base.DispatchTouchEvent(ev);
        }

        private static EditText FindTouchedEditText(Android.Views.View view, float x, float y)
        {
            if (view is EditText editText)
            {
                int[] location = new int[2];
                view.GetLocationOnScreen(location);
                float left = location[0];
                float top = location[1];
                float right = left + view.Width;
                float bottom = top + view.Height;

                if (x >= left && x <= right && y >= top && y <= bottom)
                    return editText;
            }

            if (view is ViewGroup group)
            {
                for (int i = 0; i < group.ChildCount; i++)
                {
                    var result = FindTouchedEditText(group.GetChildAt(i), x, y);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }
    }
}

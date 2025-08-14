using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Content;

namespace Gasolutions.Maui.App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.SetSoftInputMode(Android.Views.SoftInput.AdjustResize);
        }

        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            if (ev.Action == MotionEventActions.Down)
            {
                var v = CurrentFocus;
                if (v != null)
                {
                    int[] scrcoords = new int[2];
                    v.GetLocationOnScreen(scrcoords);
                    float x = ev.RawX + v.Left - scrcoords[0];
                    float y = ev.RawY + v.Top - scrcoords[1];

                    if (x < v.Left || x > v.Right || y < v.Top || y > v.Bottom)
                    {
                        var imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(v.WindowToken, 0);
                        v.ClearFocus();
                    }
                }
            }
            return base.DispatchTouchEvent(ev);
        }
    }
}

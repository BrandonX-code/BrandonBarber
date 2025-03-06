using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace Gasolutions.Maui.App.Pages
{
    public partial class ConfiguracionPage : ContentPage
    {
        public ConfiguracionPage()
        {
            InitializeComponent();
        }

        private void ThemeSwitchToggled(object sender, ToggledEventArgs e)
        {
            Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
            Preferences.Set("ModoOscuro", e.Value);
        }

        private void IdiomasPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            string idiomaSeleccionado = IdiomasPicker.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(idiomaSeleccionado))
            {
                Preferences.Set("Idioma", idiomaSeleccionado);
                CultureInfo culture = idiomaSeleccionado switch
                {
                    "Español" => new CultureInfo("es"),
                    "Inglés" => new CultureInfo("en"),
                    "Portugués" => new CultureInfo("pt"),
                    _ => CultureInfo.CurrentCulture
                };

                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
        }

        private void FontSizeSliderValueChanged(object sender, ValueChangedEventArgs e)
        {
            double newSize = e.NewValue;
            PreviewText.FontSize = newSize;

        }

        private void SonidoSwitchToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("SonidoNotificaciones", e.Value);
        }

        private void GuardarConfiguracionClicked(object sender, EventArgs e)
        {
            DisplayAlert("Configuración", "Se han guardado los cambios.", "OK");
        }
    }
}

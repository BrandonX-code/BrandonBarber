using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App
{
    public partial class InicioPages : ContentPage
    {
        public InicioPages()
        {
            InitializeComponent();

        }
        private async void MainPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage());
        }
    }
}

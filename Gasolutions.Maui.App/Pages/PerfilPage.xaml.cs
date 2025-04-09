using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App.Pages
{
    public partial class PerfilPage
    {
        public PerfilPage()
        {
            InitializeComponent();
        }

        private void OnBackTapped(object sender, TappedEventArgs e)
        {
            // Esto te devuelve a la página anterior
            Navigation.PopAsync();
        }

        private void OnEditarPerfilClicked(object sender, EventArgs e)
        {
            // Aquí puedes navegar a otra página o mostrar un diálogo para editar
            DisplayAlert("Editar Perfil", "Funcionalidad pendiente de implementar.", "OK");
        }
    }
}

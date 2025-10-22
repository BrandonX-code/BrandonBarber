using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Models
{
    public class PerfilActualizadoMessage : ValueChangedMessage<UsuarioModels>
    {
        public PerfilActualizadoMessage(UsuarioModels value) : base(value) { }
    }
}

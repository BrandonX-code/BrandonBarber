using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Gasolutions.Maui.App.Messenger
{

    public class CalificacionEnviadaMessage : ValueChangedMessage<long>
    {
        public CalificacionEnviadaMessage(long barberoId) : base(barberoId) { }
    }

}

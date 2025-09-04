using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Barber.Maui.BrandonBarber.Messenger
{

    public class CalificacionEnviadaMessage(long barberoId) : ValueChangedMessage<long>(barberoId)
    {
    }

}

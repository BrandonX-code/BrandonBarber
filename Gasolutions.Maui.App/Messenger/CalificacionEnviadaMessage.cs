using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Gasolutions.Maui.App.Messenger
{

    public class CalificacionEnviadaMessage(long barberoId) : ValueChangedMessage<long>(barberoId)
    {
    }

}

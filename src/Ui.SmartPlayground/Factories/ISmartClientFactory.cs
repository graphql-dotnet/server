

using GraphQL.Server.Ui.SmartPlayground.Smart;

namespace GraphQL.Server.Ui.SmartPlayground.Factories
{
    public interface ISmartClientFactory
    {
        ISmartClient CreateClient(SmartPlaygroundOptions options);
    }
}

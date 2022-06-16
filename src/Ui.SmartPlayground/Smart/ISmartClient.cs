namespace GraphQL.Server.Ui.SmartPlayground.Smart
{
    public interface ISmartClient
    {
        Task Launch();
        Task<string> Redirect(string code);
    }
}

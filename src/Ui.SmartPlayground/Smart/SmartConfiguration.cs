namespace GraphQL.Server.Ui.SmartPlayground.Smart
{
    public class SmartConfiguration
    {
        public Uri? Authorization { private set; get; }
        public Uri? Token { private set; get; }
        public string ClientId { private set; get; }
        public Uri? RedirectUri { private set; get; }
        public string Scope { private set; get; }

        public SmartConfiguration(Uri authorization, Uri token, string clientId, Uri redirectUri, string scope)
        {
            Authorization = authorization;
            Token = token;
            ClientId = clientId;
            RedirectUri = redirectUri;
            Scope = scope;
        }
    }
}

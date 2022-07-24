namespace GraphQL.Server.Ui.SmartPlayground.Smart
{
    public class OAuth2Settings
    {
        public const string OAuth2 = "OAuth2";

        public bool IsEnabled { get; set; }
        public Uri? AuthorizeUrl { get; set; }
        public Uri? TokenUrl { get; set; }
        public Uri? BaseUrl { get; set; }

        public Uri SafeAuthorizeUrl
        {
            get
            {
                if (AuthorizeUrl == null)
                {
                    throw new InvalidDataException("Missing AuthorizeUrl in OAuth2 section in configuration!");
                }

                return AuthorizeUrl;
            }
        }

        public Uri SafeTokenUrl
        {
            get
            {
                if (TokenUrl == null)
                {
                    throw new InvalidDataException("Missing SafeTokenUrl in OAuth2 section in configuration!");
                }

                return TokenUrl;
            }
        }

        public Uri SafeBaseUrl
        {
            get
            {
                if (BaseUrl == null)
                {
                    throw new InvalidDataException("Missing BaseUrl in OAuth2 section in configuration!");
                }

                var baseUrlString = BaseUrl.ToString();
                var result = new Uri(baseUrlString.EndsWith("/") ? baseUrlString : baseUrlString + "/");

                return result;
            }
        }
    }
}

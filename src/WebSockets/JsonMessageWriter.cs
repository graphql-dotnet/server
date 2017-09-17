using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.WebSockets
{
    public class JsonMessageWriter : IJsonMessageWriter
    {
        private readonly IWebSocketMessageClient _socketMessageClient;

        public JsonMessageWriter(IWebSocketMessageClient socketMessageClient)
        {
            _socketMessageClient = socketMessageClient;
        }

        public Task WriteMessageAsync<T>(T message)
        {
            var messageJson = JsonConvert.SerializeObject(message);

            return _socketMessageClient.WriteMessageAsync(messageJson);
        }
    }
}

using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.WebSockets.Messages
{
    public class JsonMessageWriter : IJsonMessageWriter
    {
        private readonly IWebSocketClient _socketClient;

        public JsonMessageWriter(IWebSocketClient socketClient)
        {
            _socketClient = socketClient;
        }

        public Task WriteMessageAsync<T>(T message)
        {
            var messageJson = JsonConvert.SerializeObject(message);

            return _socketClient.WriteMessageAsync(messageJson);
        }
    }
}

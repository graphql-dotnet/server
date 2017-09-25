using System;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.WebSockets
{
    public class JsonMessageReader : IJsonMessageReader
    {
        private readonly IWebSocketClient _socketClient;

        public JsonMessageReader(IWebSocketClient socketClient)
        {
            _socketClient = socketClient;
        }

        public async Task<T> ReadMessageAsync<T>()
        {
            try
            {
                var message = await _socketClient.ReadMessageAsync();

                if (message == null)
                    return default(T);

                var messageObject = JsonConvert.DeserializeObject<T>(message);
                return messageObject;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}

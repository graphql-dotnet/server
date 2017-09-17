using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.WebSockets
{
    public class JsonMessageReader : IJsonMessageReader
    {
        private readonly IWebSocketMessageClient _socketMessageClient;

        public JsonMessageReader(IWebSocketMessageClient socketMessageClient)
        {
            _socketMessageClient = socketMessageClient;
        }

        public async Task<T> ReadMessageAsync<T>()
        {
            try
            {
                var message = await _socketMessageClient.ReadMessageAsync();

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

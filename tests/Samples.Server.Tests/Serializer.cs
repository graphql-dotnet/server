#if NETCOREAPP2_2
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Samples.Server.Tests
{
    public static class Serializer
    {
        public static string Serialize(object obj)
#if NETCOREAPP2_2
            => JsonConvert.SerializeObject(obj);
#else
            => JsonSerializer.Serialize(obj);
#endif
    }
}

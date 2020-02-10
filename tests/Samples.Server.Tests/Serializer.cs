#if NETCOREAPP2_2
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Samples.Server.Tests
{
    internal static class Serializer
    {
        internal static string Serialize(object obj)
#if NETCOREAPP2_2
            => JsonConvert.SerializeObject(obj);
#else
            => JsonSerializer.Serialize(obj);
#endif
    }
}

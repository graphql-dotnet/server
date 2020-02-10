#if NETSTANDARD2_0
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace GraphQL.Server.Ui
{
    internal static class Serializer
    {
        internal static string Serialize(object obj)
#if NETSTANDARD2_0
            => JsonConvert.SerializeObject(obj);
#else
            => JsonSerializer.Serialize(obj);
#endif
    }
}

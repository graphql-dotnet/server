using Newtonsoft.Json.Linq;
using Shouldly;

namespace Samples.Server.Tests
{
    internal static class ShouldBeExtensions
    {
        public static void ShouldBe(this string actual, string expected, bool ignoreExtensions)
        {
            if (ignoreExtensions)
            {
                if (actual.StartsWith('['))
                {
                    var json = JArray.Parse(actual);
                    foreach (var item in json)
                    {
                        if (item is JObject obj)
                        {
                            obj.Remove("extensions");
                        }
                    }
                    actual = json.ToString(Newtonsoft.Json.Formatting.None);
                }
                else
                {
                    var json = JObject.Parse(actual);
                    json.Remove("extensions");
                    actual = json.ToString(Newtonsoft.Json.Formatting.None);
                }
            }

            actual.ShouldBe(expected);
        }
    }
}

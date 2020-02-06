using Newtonsoft.Json.Linq;
using Shouldly;

namespace Samples.Server.Tests
{
    internal static class ShouldBeExtensions
    {
        public static void ShouldBeEquivalentJson(this string actual, string expected, bool ignoreExtensions)
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

            actual.ShouldBeEquivalentJson(expected);
        }

        /// <summary>
        /// Comparison two strings but replaces any equivalent encoding differences first.
        /// </summary>
        /// <param name="actualJson"></param>
        /// <param name="expectedJson"></param>
        public static void ShouldBeEquivalentJson(this string actualJson, string expectedJson)
        {
#if !NETCOREAPP2_2 // e.g. System.Text.Json is being used which encodes slightly differently
            expectedJson = expectedJson.Replace("'", @"\u0027");
#endif

            actualJson.ShouldBe(expectedJson);
        }
    }
}

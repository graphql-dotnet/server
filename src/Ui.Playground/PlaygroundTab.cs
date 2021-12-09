using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GraphQL.Server.Ui.Playground
{
    /// <summary>
    /// A tab for GraphQL Playground UI.
    /// </summary>
    public class PlaygroundTab
    {
        public PlaygroundTab(string query)
        {
            Query = query;
        }

        /// <summary>
        /// Endpoint with which the tab will be initialized.
        /// </summary>
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = "/graphql";

        /// <summary>
        /// Query with which the tab will be initialized.
        /// </summary>
        [JsonPropertyName("query")]
        public string Query { get; set; }

        /// <summary>
        /// The tab name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Variables with which the tab will be initialized.
        /// </summary>
        [JsonPropertyName("variables")]
        public string? Variables { get; set; }

        /// <summary>
        /// Responses with which the tab will be initialized.
        /// </summary>
        [JsonPropertyName("responses")]
        public IEnumerable<string>? Responses { get; set; }

        /// <summary>
        /// HTTP headers with which the tab will be initialized.
        /// </summary>
        [JsonPropertyName("headers")]
        public Dictionary<string, object>? Headers { get; set; }
    }
}

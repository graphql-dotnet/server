using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GraphQL.Server.Samples.Jwt;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }

    public string GraphQLEndPoint => "/graphql";

    public string SubscriptionsEndPoint => "/graphql";

    public IDictionary<string, object?> Headers { get; } = new Dictionary<string, object?> {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" },
        };

    public string GraphiQLElement => "GraphiQLWithExtensions.GraphiQLWithExtensions";

    public bool HeaderEditorEnabled => true;
}

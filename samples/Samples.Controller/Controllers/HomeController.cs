using GraphQL;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Mvc;

namespace ControllerSample.Controllers;

public class HomeController : Controller
{
    private readonly IDocumentExecuter<ISchema> _executer;
    private readonly IGraphQLTextSerializer _serializer;
    private readonly IUserContextBuilder _websocketUserContextBuilder;

    public HomeController(IDocumentExecuter<ISchema> executer, IGraphQLTextSerializer serializer)
    {
        _executer = executer;
        _serializer = serializer;
        _websocketUserContextBuilder = new UserContextBuilder<IDictionary<string, object?>>((_, _) => new Dictionary<string, object?>());
    }

    public IActionResult Index()
        => View();

    [HttpGet]
    [ActionName("graphql")]
    public Task<IActionResult> GraphQLGetAsync(string query, string? operationName)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            return Task.FromResult<IActionResult>(BadRequest());
        }
        else
        {
            return ExecuteGraphQLRequestAsync(BuildRequest(query, operationName));
        }
    }

    [HttpPost]
    [ActionName("graphql")]
    public async Task<IActionResult> GraphQLPostAsync()
    {
        if (HttpContext.Request.HasFormContentType)
        {
            var form = await HttpContext.Request.ReadFormAsync(HttpContext.RequestAborted);
            return await ExecuteGraphQLRequestAsync(BuildRequest(form["query"].ToString(), form["operationName"].ToString(), form["variables"].ToString(), form["extensions"].ToString()));
        }
        else if (HttpContext.Request.HasJsonContentType())
        {
            var request = await _serializer.ReadAsync<GraphQLRequest>(HttpContext.Request.Body, HttpContext.RequestAborted);
            return await ExecuteGraphQLRequestAsync(request);
        }
        return BadRequest();
    }

    private GraphQLRequest BuildRequest(string query, string? operationName, string? variables = null, string? extensions = null)
        => new GraphQLRequest
        {
            Query = query == "" ? null : query,
            OperationName = operationName == "" ? null : operationName,
            Variables = _serializer.Deserialize<Inputs>(variables == "" ? null : variables),
            Extensions = _serializer.Deserialize<Inputs>(extensions == "" ? null : extensions),
        };

    private async Task<IActionResult> ExecuteGraphQLRequestAsync(GraphQLRequest? request)
    {
        try
        {
            var opts = new ExecutionOptions
            {
                Query = request?.Query,
                OperationName = request?.OperationName,
                Variables = request?.Variables,
                Extensions = request?.Extensions,
                CancellationToken = HttpContext.RequestAborted,
                RequestServices = HttpContext.RequestServices,
            };
            IValidationRule rule = HttpMethods.IsGet(HttpContext.Request.Method) ? new HttpGetValidationRule() : new HttpPostValidationRule();
            opts.ValidationRules = DocumentValidator.CoreRules.Append(rule);
            opts.CachedDocumentValidationRules = new[] { rule };
            return new ExecutionResultActionResult(await _executer.ExecuteAsync(opts));
        }
        catch
        {
            return BadRequest();
        }
    }
}

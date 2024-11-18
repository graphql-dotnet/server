using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Ui.GraphiQL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Samples.AzureFunctions;

public class GraphQL
{
    [FunctionName("GraphQL")]
    public static IActionResult RunGraphQL(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a GraphQL request.");

        return new GraphQLExecutionActionResult();
    }

    [FunctionName("GraphiQL")]
    public static IActionResult RunGraphiQL(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest request,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request for the GraphiQL UI.");

        return new GraphiQLActionResult(opts => opts.GraphQLEndPoint = "/api/graphql"); // /api/graphql route will call RunGraphQL method
    }
}

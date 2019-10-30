using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GraphQL;
using GraphQL.Server.Internal;
using Demo.Azure.Functions.GraphQL.Schema;
using Demo.Azure.Functions.GraphQL.Infrastructure;

namespace Demo.Azure.Functions.GraphQL
{
    public class GraphQLFunction
    {
        private readonly IGraphQLExecuter<StarWarsSchema> _graphQLExecuter;

        public GraphQLFunction(IGraphQLExecuter<StarWarsSchema> graphQLExecuter)
        {
            _graphQLExecuter = graphQLExecuter ?? throw new ArgumentNullException(nameof(graphQLExecuter));
        }

        [FunctionName("graphql")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request,
            ILogger logger)
        {
            try
            {
                ExecutionResult executionResult = await _graphQLExecuter.ExecuteAsync(request);

                if (executionResult.Errors != null)
                {
                    logger.LogError("GraphQL execution error(s): {Errors}", executionResult.Errors);
                }

                return new GraphQLExecutionResult(executionResult);
            }
            catch (GraphQLBadRequestException ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }
    }
}

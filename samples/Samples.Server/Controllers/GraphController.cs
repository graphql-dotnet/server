using System.Text;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Instrumentation;
using GraphQL.Samples.Schemas.Chat;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Samples.Server.Controllers
{
    [Route("graphql")]
    public class GraphController : Controller
    {
        private readonly IDocumentExecuter _executer;
        private readonly Schema _schema;
        private readonly IDocumentWriter _writer;

        public GraphController(
            IDocumentExecuter executer,
            IDocumentWriter writer,
            ChatSchema schema)
        {
            _executer = executer;
            _writer = writer;
            _schema = schema;
        }

        [HttpPost("query")]
        public async Task<ContentResult> PostAsync([FromBody] GraphQuery q)
        {
            var inputs = q.GetInputs();
            var results = await _executer.ExecuteAsync(options =>
            {
                options.Query = q.Query;
                options.Schema = _schema;
                options.Inputs = inputs;
                options.UserContext = null;
                options.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
            });

            var json = _writer.Write(results);
            return Content(json, "application/json", Encoding.UTF8);
        }
    }
}

using GraphQLParser.AST;

namespace Tests.Middleware;

public class FileUploadTests : IDisposable
{
    private readonly TestServer _server;

    public FileUploadTests()
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<FileGraphType>();
            services.AddGraphQL(b => b
                .AddSchema<MySchema>()
                .AddSystemTextJson());
#if NETCOREAPP2_1 || NET48
            services.AddHostApplicationLifetime();
#endif
        });
        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            app.UseGraphQL<MyMiddleware>("/graphql", new GraphQLHttpMiddlewareOptions());
        });
        _server = new TestServer(hostBuilder);
        _server.Host.Services.GetRequiredService<ISchema>().Initialize();
    }

    public void Dispose() => _server.Dispose();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Basic(bool withOtherVariables)
    {
        var client = _server.CreateClient();
        var content = new MultipartFormDataContent();
        var queryContent = new StringContent(@"query($prefix: String, $file: File!) { convertToBase64(prefix: $prefix, file: $file) }");
        queryContent.Headers.ContentType = new("application/graphql");
        content.Add(queryContent, "query");
        if (withOtherVariables)
        {
            var variablesContent = new StringContent(@"{""prefix"":""pre-""}");
            variablesContent.Headers.ContentType = new("application/json");
            content.Add(variablesContent, "variables");
        }
        var fileData = Encoding.UTF8.GetBytes("abcd");
        var fileContent = new ByteArrayContent(fileData);
        fileContent.Headers.ContentType = new("application/octet-stream");
        content.Add(fileContent, "file", "filename.bin");
        using var response = await client.PostAsync("/graphql", content);
        if (withOtherVariables)
        {
            await response.ShouldBeAsync(@"{""data"":{""convertToBase64"":""pre-filename.bin-YWJjZA==""}}");
        }
        else
        {
            await response.ShouldBeAsync(@"{""data"":{""convertToBase64"":""filename.bin-YWJjZA==""}}");
        }
    }

    public class MyMiddleware : GraphQLHttpMiddleware<MySchema>
    {
        private readonly IGraphQLTextSerializer _serializer;

        public MyMiddleware(RequestDelegate next, IGraphQLTextSerializer serializer, IDocumentExecuter<MySchema> documentExecuter, IServiceScopeFactory serviceScopeFactory, GraphQLHttpMiddlewareOptions options, IHostApplicationLifetime hostApplicationLifetime)
            : base(next, serializer, documentExecuter, serviceScopeFactory, options, hostApplicationLifetime)
        {
            _serializer = serializer;
        }

        protected override async Task<(GraphQLRequest? SingleRequest, IList<GraphQLRequest?>? BatchRequest)?> ReadPostContentAsync(
            HttpContext context, RequestDelegate next, string? mediaType, Encoding? sourceEncoding)
        {
            if (context.Request.HasFormContentType)
            {
                try
                {
                    var formCollection = await context.Request.ReadFormAsync(context.RequestAborted);
                    return (DeserializeFromFormBody(formCollection), null);
                }
                catch (Exception ex)
                {
                    if (!await HandleDeserializationErrorAsync(context, next, ex))
                        throw;
                    return null;
                }
            }
            return await base.ReadPostContentAsync(context, next, mediaType, sourceEncoding);
        }

        private GraphQLRequest DeserializeFromFormBody(IFormCollection formCollection)
        {
            var request = new GraphQLRequest
            {
                Query = formCollection.TryGetValue("query", out var queryValues) ? queryValues[0] : null,
                Variables = formCollection.TryGetValue("variables", out var variablesValues) ? _serializer.Deserialize<Inputs>(variablesValues[0]) : null,
                Extensions = formCollection.TryGetValue("extensions", out var extensionsValues) ? _serializer.Deserialize<Inputs>(extensionsValues[0]) : null,
                OperationName = formCollection.TryGetValue("operationName", out var operationNameValues) ? operationNameValues[0] : null,
            };
            if (formCollection.Files.Count > 0)
            {
                var dic = request.Variables != null ? new Dictionary<string, object?>(request.Variables) : new Dictionary<string, object?>();
                foreach (var file in formCollection.Files)
                {
                    dic.Add(file.Name, file);
                }
                request.Variables = new Inputs(dic);
            }
            return request;
        }
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            var query = new ObjectGraphType
            {
                Name = "Query",
            };
            query.Field<StringGraphType>("ConvertToBase64")
                .Argument<StringGraphType>("prefix")
                .Argument<NonNullGraphType<FileGraphType>>("file")
                .Resolve(context =>
                {
                    var prefix = context.GetArgument<string?>("prefix");
                    var file = context.GetArgument<IFormFile>("file");
                    var memStream = new MemoryStream();
                    file.CopyTo(memStream);
                    var bytes = memStream.ToArray();
                    return prefix + file.FileName + "-" + Convert.ToBase64String(bytes);
                });
            Query = query;
        }
    }

    public class FileGraphType : ScalarGraphType
    {
        public FileGraphType()
        {
            Name = "File";
        }

        public override object? ParseLiteral(GraphQLValue value)
            => value is GraphQLNullValue ? null : ThrowLiteralConversionError(value);

        public override object? ParseValue(object? value) => value switch
        {
            null => null,
            IFormFile => value,
            _ => ThrowValueConversionError(value),
        };

        public override object? Serialize(object? value)
            => throw new InvalidOperationException("This scalar does not support serialization.");
    }
}

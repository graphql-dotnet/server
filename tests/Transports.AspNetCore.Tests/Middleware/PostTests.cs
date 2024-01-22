using System.Net;

namespace Tests.Middleware;

public class PostTests : IDisposable
{
    private GraphQLHttpMiddlewareOptions _options = null!;
    private GraphQLHttpMiddlewareOptions _options2 = null!;
    private readonly Action<ExecutionOptions> _configureExecution = _ => { };
    private readonly TestServer _server;

    public PostTests()
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddAutoSchema<Chat.Query>(s => s
                    .WithMutation<Chat.Mutation>()
                    .WithSubscription<Chat.Subscription>())
                .AddSchema<Schema2>()
                .AddAutoClrMappings()
                .AddFormFileGraphType()
                .AddSystemTextJson()
                .ConfigureExecutionOptions(o => _configureExecution(o)));
#if NETCOREAPP2_1 || NET48
            services.AddHostApplicationLifetime();
#endif
        });
        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            app.UseGraphQL("/graphql", opts =>
            {
                _options = opts;
            });
            app.UseGraphQL<Schema2>("/graphql2", opts =>
            {
                _options2 = opts;
            });
        });
        _server = new TestServer(hostBuilder);
    }

    private class Schema2 : Schema
    {
        public Schema2(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Query = new AutoRegisteringObjectGraphType<Query2>();
        }
    }

    private class Query2
    {
        public static string? Var(string? test) => test;

        public static string? Ext(IResolveFieldContext context)
            => context.InputExtensions.TryGetValue("test", out var value) ? value?.ToString() : null;

        public static MyFile? File(IFormFile? file) => file == null ? null : new(file);

        public static IEnumerable<MyFile> File2(IEnumerable<IFormFile> files) => files.Select(x => new MyFile(x));
        public static MyFile File3(MyFileInput arg) => new(arg.File);
        public static IEnumerable<MyFile> File4(IEnumerable<MyFileInput> args) => args.Select(x => new MyFile(x.File));
        public static IEnumerable<MyFile> File5(MyFileInput2 args) => args.Files.Select(x => new MyFile(x));
    }

    private record MyFileInput(IFormFile File);
    private record MyFileInput2(IEnumerable<IFormFile> Files);

    private class MyFile
    {
        private readonly IFormFile _file;
        public MyFile(IFormFile file)
        {
            _file = file;
        }

        public string Name => _file.Name;
        public string ContentType => _file.ContentType;
        public string Content()
        {
            using var stream = _file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }

    public void Dispose() => _server.Dispose();

    private Task<HttpResponseMessage> PostJsonAsync(string url, string json)
    {
        var client = _server.CreateClient();
        var content = new StringContent(json);
        content.Headers.ContentType = new("application/json");
        return client.PostAsync(url, content);
    }

    private Task<HttpResponseMessage> PostJsonAsync(string json)
        => PostJsonAsync("/graphql", json);

    private Task<HttpResponseMessage> PostRequestAsync(GraphQLRequest request)
        => PostJsonAsync(new GraphQLSerializer().Serialize(request));

    private Task<HttpResponseMessage> PostRequestAsync(string url, GraphQLRequest request)
        => PostJsonAsync(url, new GraphQLSerializer().Serialize(request));

    [Fact]
    public async Task BasicTest()
    {
        using var response = await PostRequestAsync(new() { Query = "{count}" });
        await response.ShouldBeAsync("""{"data":{"count":0}}""");
    }

#if NET5_0_OR_GREATER
    [Fact]
    public async Task AltCharset()
    {
        var client = _server.CreateClient();
        var content = new StringContent("""{"query":"{var(test:\"ë\")}"}""", Encoding.Latin1, "application/json");
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync(false, """{"data":{"var":"\u00EB"}}""");
    }

    [Fact]
    public async Task AltCharset_Quoted()
    {
        var client = _server.CreateClient();
        var bytes = Encoding.UTF8.GetBytes("""{"query":"{var(test:\"ë\")}"}""");
        var content = new ByteArrayContent(bytes, 0, bytes.Length);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "\"UTF-8\"" };
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync(false, """{"data":{"var":"\u00EB"}}""");
    }

    [Fact]
    public async Task AltCharset_Invalid()
    {
        var client = _server.CreateClient();
        var bytes = Encoding.UTF8.GetBytes("""{"query":"{var(test:\"ë\")"}""");
        var content = new ByteArrayContent(bytes, 0, bytes.Length);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "unknown" };
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync(HttpStatusCode.UnsupportedMediaType, """{"errors":[{"message":"Invalid \u0027Content-Type\u0027 header: value \u0027application/json; charset=unknown\u0027 could not be parsed.","extensions":{"code":"INVALID_CONTENT_TYPE","codes":["INVALID_CONTENT_TYPE"]}}]}""");
    }
#endif

    [Fact]
    public async Task FormMultipart_Legacy()
    {
        var client = _server.CreateClient();
        var content = new MultipartFormDataContent();
        var queryContent = new StringContent("query op1{ext} query op2($test:String!){ext var(test:$test)}");
        queryContent.Headers.ContentType = new("application/graphql");
        var variablesContent = new StringContent("""{"test":"1"}""");
        variablesContent.Headers.ContentType = new("application/json");
        var extensionsContent = new StringContent("""{"test":"2"}""");
        extensionsContent.Headers.ContentType = new("application/json");
        var operationNameContent = new StringContent("op2");
        operationNameContent.Headers.ContentType = new("text/text");
        content.Add(queryContent, "query");
        content.Add(variablesContent, "variables");
        content.Add(extensionsContent, "extensions");
        content.Add(operationNameContent, "operationName");
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync("""{"data":{"ext":"2","var":"1"}}""");
    }

    [Fact]
    public async Task FormMultipart_Upload()
    {
        var client = _server.CreateClient();
        var content = new MultipartFormDataContent();
        var jsonContent = new StringContent("""
            {
                "query": "query op1{ext} query op2($test:String!){ext var(test:$test)}",
                "operationName": "op2",
                "variables": { "test": "1" },
                "extensions": { "test": "2"}
            }
            """, Encoding.UTF8, "application/json");
        content.Add(jsonContent, "operations");
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync("""{"data":{"ext":"2","var":"1"}}""");
    }

    // successful queries
    // typical, single file
    [InlineData("{\"query\":\"query($arg:FormFile){file(file:$arg){name contentType content}}\",\"variables\":{\"arg\":null}}", "{\"file0\":[\"variables.arg\"]}", true, false,
        200, "{\"data\":{\"file\":{\"name\":\"file0\",\"contentType\":\"text/text; charset=utf-8\",\"content\":\"test1\"}}}")]
    // single file with map specified as 0.variables
    [InlineData("{\"query\":\"query($arg:FormFile){file(file:$arg){content}}\",\"variables\":{\"arg\":null}}", "{\"file0\":[\"0.variables.arg\"]}", true, false,
        200, "{\"data\":{\"file\":{\"content\":\"test1\"}}}")]
    // two files
    [InlineData("{\"query\":\"query($arg1:FormFile,$arg2:FormFile){file0:file(file:$arg1){content},file1:file(file:$arg2){content}}\",\"variables\":{\"arg1\":null,\"arg2\":null}}", "{\"file0\":[\"0.variables.arg1\"],\"file1\":[\"0.variables.arg2\"]}", true, true,
        200, "{\"data\":{\"file0\":{\"content\":\"test1\"},\"file1\":{\"content\":\"test2\"}}}")]
    // batch query of two requests
    [InlineData("[{\"query\":\"query($arg:FormFile){file(file:$arg){content}}\",\"variables\":{\"arg\":null}},{\"query\":\"query($arg:FormFile){file(file:$arg){content}}\",\"variables\":{\"arg\":null}}]", "{\"file0\":[\"0.variables.arg\"],\"file1\":[\"1.variables.arg\"]}", true, true,
        200, "[{\"data\":{\"file\":{\"content\":\"test1\"}}},{\"data\":{\"file\":{\"content\":\"test2\"}}}]")]
    // batch query of one request
    [InlineData("[{\"query\":\"query($arg:FormFile){file(file:$arg){content}}\",\"variables\":{\"arg\":null}}]", "{\"file0\":[\"variables.arg\"]}", true, false,
        200, "[{\"data\":{\"file\":{\"content\":\"test1\"}}}]")]
    // referencing a variable's child by index
    [InlineData("{\"query\":\"query($arg:[FormFile!]!){file2(files:$arg){content}}\",\"variables\":{\"arg\":[null]}}", "{\"file0\":[\"variables.arg.0\"]}", true, false,
        200, "{\"data\":{\"file2\":[{\"content\":\"test1\"}]}}")]
    // referencing a variable's child by property name
    [InlineData("{\"query\":\"query($arg:MyFileInput!){file3(arg:$arg){content}}\",\"variables\":{\"arg\":{\"file\":null}}}", "{\"file0\":[\"variables.arg.file\"]}", true, false,
        200, "{\"data\":{\"file3\":{\"content\":\"test1\"}}}")]
    // referencing a variable's child by index by property name
    [InlineData("{\"query\":\"query($arg:[MyFileInput!]!){file4(args:$arg){content}}\",\"variables\":{\"arg\":[{\"file\":null}]}}", "{\"file0\":[\"variables.arg.0.file\"]}", true, false,
        200, "{\"data\":{\"file4\":[{\"content\":\"test1\"}]}}")]

    // failing queries
    // invalid index for request (string not integer)
    [InlineData(null, "{\"file0\":[\"abc.variables.arg\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Could not parse the request index.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid index for request
    [InlineData(null, "{\"file0\":[\"1.variables.arg\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Invalid request index.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // already set variable
    [InlineData("{\"query\":\"query($arg:FormFile){file(file:$arg){content}}\",\"variables\":{\"arg\":\"hello\"}}", "{\"file0\":[\"variables.arg\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027arg\\u0027 must refer to a null object.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid 'operations' json
    [InlineData("{", null, false, false,
        400, "{\"errors\":[{\"message\":\"JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1.\",\"extensions\":{\"code\":\"JSON_INVALID\",\"codes\":[\"JSON_INVALID\"]}}]}")]
    // invalid 'map' json
    [InlineData(null, "{", false, false,
        400, "{\"errors\":[{\"message\":\"JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1.\",\"extensions\":{\"code\":\"JSON_INVALID\",\"codes\":[\"JSON_INVALID\"]}}]}")]
    // invalid map path: invalid prefix
    [InlineData(null, "{\"file0\":[\"abc\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map path must start with \\u0027variables.\\u0027 or the index of the request followed by \\u0027.variables.\\u0027.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"file0\":[\"0.abc\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map path must start with \\u0027variables.\\u0027 or the index of the request followed by \\u0027.variables.\\u0027.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"file0\":[\"variables\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map path must start with \\u0027variables.\\u0027 or the index of the request followed by \\u0027.variables.\\u0027.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"file0\":[\"0.variables\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map path must start with \\u0027variables.\\u0027 or the index of the request followed by \\u0027.variables.\\u0027.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid map path: missing property name
    [InlineData(null, "{\"file0\":[\"variables.\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Empty property name.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"file0\":[\"0.variables.\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Empty property name.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid map path: child of null specified
    [InlineData(null, "{\"file0\":[\"variables.arg.file\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027arg\\u0027 refers to a null object.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid map path: child of string specified
    [InlineData("{\"query\":\"query($arg:FormFile){file(file:$arg){name contentType content}}\",\"variables\":{\"arg\":\"hello\"}}", "{\"file0\":[\"variables.arg.file\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Cannot refer to child property \\u0027file\\u0027 of a string or number.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid map keys
    [InlineData(null, "{\"\":[\"0.variables.arg\"]}", false, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map key cannot be query, operationName, variables, extensions, operations or map.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"query\":[\"0.variables.arg\"]}", false, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map key cannot be query, operationName, variables, extensions, operations or map.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"variables\":[\"0.variables.arg\"]}", false, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map key cannot be query, operationName, variables, extensions, operations or map.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"extensions\":[\"0.variables.arg\"]}", false, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map key cannot be query, operationName, variables, extensions, operations or map.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"operationName\":[\"0.variables.arg\"]}", false, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map key cannot be query, operationName, variables, extensions, operations or map.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [InlineData(null, "{\"map\":[\"0.variables.arg\"]}", false, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map key cannot be query, operationName, variables, extensions, operations or map.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // missing referenced file
    [InlineData(null, "{\"file0\":[\"0.variables.arg\"]}", false, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Map key does not refer to an uploaded file.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // no variables in request
    [InlineData("{}", "{\"file0\":[\"0.variables.arg\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. No variables defined for this request.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // variables present but not the one referenced
    [InlineData(null, "{\"file0\":[\"0.variables.arg2\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027arg2\\u0027 does not exist.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid variable path
    [InlineData(null, "{\"file0\":[\"0.variables.arg.child\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027arg\\u0027 refers to a null object.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // file2 tests
    // missing index in variable path
    [InlineData("{\"query\":\"query($arg:[FormFile!]!){file2(files:$arg){content}}\",\"variables\":{\"arg\":[null]}}", "{\"file0\":[\"variables.arg\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027arg\\u0027 must refer to a null object.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid index in variable path
    [InlineData("{\"query\":\"query($arg:[FormFile!]!){file2(files:$arg){content}}\",\"variables\":{\"arg\":[null]}}", "{\"file0\":[\"variables.arg.1\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Index \\u00271\\u0027 is out of bounds.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // name instead of index in variable path
    [InlineData("{\"query\":\"query($arg:[FormFile!]!){file2(files:$arg){content}}\",\"variables\":{\"arg\":[null]}}", "{\"file0\":[\"variables.arg.abc\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child index \\u0027abc\\u0027 is not an integer.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // suffix in variable path
    [InlineData("{\"query\":\"query($arg:[FormFile!]!){file2(files:$arg){content}}\",\"variables\":{\"arg\":[null]}}", "{\"file0\":[\"variables.arg.0.abc\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child index \\u00270\\u0027 refers to a null object.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // suffix in variable path for string
    [InlineData("{\"query\":\"query($arg:[FormFile!]!){file2(files:$arg){content}}\",\"variables\":{\"arg\":[\"test\"]}}", "{\"file0\":[\"variables.arg.0.abc\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Cannot refer to child property \\u0027abc\\u0027 of a string or number.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // already set variable
    [InlineData("{\"query\":\"query($arg:[FormFile!]!){file2(files:$arg){content}}\",\"variables\":{\"arg\":[\"test\"]}}", "{\"file0\":[\"variables.arg.0\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Index \\u00270\\u0027 must refer to a null variable.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // file3 tests
    // missing prop in variable path
    [InlineData("{\"query\":\"query($arg:[FormFile!]!){file3(arg:$arg){content}}\",\"variables\":{\"arg\":{\"file\":null}}}", "{\"file0\":[\"variables.arg\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027arg\\u0027 must refer to a null object.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // invalid prop in variable path
    [InlineData("{\"query\":\"query($arg:MyFileInput!){file3(arg:$arg){content}}\",\"variables\":{\"arg\":{\"file\":null}}}", "{\"file0\":[\"variables.arg.1\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u00271\\u0027 does not exist.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // suffix in variable path
    [InlineData("{\"query\":\"query($arg:MyFileInput!){file3(arg:$arg){content}}\",\"variables\":{\"arg\":{\"file\":null}}}", "{\"file0\":[\"variables.arg.file.abc\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027file\\u0027 refers to a null object.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // suffix in variable path for string
    [InlineData("{\"query\":\"query($arg:MyFileInput!){file3(arg:$arg){content}}\",\"variables\":{\"arg\":{\"file\":\"test\"}}}", "{\"file0\":[\"variables.arg.file.abc\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Cannot refer to child property \\u0027abc\\u0027 of a string or number.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // already set variable
    [InlineData("{\"query\":\"query($arg:MyFileInput!){file3(arg:$arg){content}}\",\"variables\":{\"arg\":{\"file\":\"test\"}}}", "{\"file0\":[\"variables.arg.file\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027file\\u0027 must refer to a null object.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // file4 tests
    // parent not an integer
    [InlineData("{\"query\":\"query($arg:[MyFileInput!]!){file4(args:$arg){content}}\",\"variables\":{\"arg\":[{\"file\":null}]}}", "{\"file0\":[\"variables.arg.test.file\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child index \\u0027test\\u0027 is not an integer.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // parent not valid
    [InlineData("{\"query\":\"query($arg:[MyFileInput!]!){file4(args:$arg){content}}\",\"variables\":{\"arg\":[{\"file\":null}]}}", "{\"file0\":[\"variables.arg.1.file\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Index \\u00271\\u0027 is out of bounds.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    // file5 tests
    // parent not valid
    [InlineData("{\"query\":\"query($arg:MyFileInput2!){file5(arg:$arg){content}}\",\"variables\":{\"arg\":{\"files\":[null]}}}", "{\"file0\":[\"variables.arg.dummy.0\"]}", true, false,
        400, "{\"errors\":[{\"message\":\"Invalid map path. Child property \\u0027dummy\\u0027 does not exist.\",\"extensions\":{\"code\":\"INVALID_MAP\",\"codes\":[\"INVALID_MAP\"]}}]}")]
    [Theory]
    public async Task FormMultipart_Upload_Matrix(string? operations, string? map, bool file0, bool file1, int expectedStatusCode, string expectedResponse)
    {
        operations ??= "{\"query\":\"query($arg:FormFile){file(file:$arg){content}}\",\"variables\":{\"arg\":null}}";
        var client = _server.CreateClient();
        var content = new MultipartFormDataContent();
        if (operations != null)
            content.Add(new StringContent(operations, Encoding.UTF8, "application/json"), "operations");
        if (map != null)
            content.Add(new StringContent(map, Encoding.UTF8, "application/json"), "map");
        if (file0)
            content.Add(new StringContent("test1", Encoding.UTF8, "text/text"), "file0", "example1.txt");
        if (file1)
            content.Add(new StringContent("test2", Encoding.UTF8, "text/html"), "file1", "example2.html");
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync((HttpStatusCode)expectedStatusCode, expectedResponse);
    }

    [InlineData(1, null, HttpStatusCode.RequestEntityTooLarge, "{\"errors\":[{\"message\":\"File uploads exceeded.\",\"extensions\":{\"code\":\"FILE_COUNT_EXCEEDED\",\"codes\":[\"FILE_COUNT_EXCEEDED\"]}}]}")]
    [InlineData(null, 1, HttpStatusCode.RequestEntityTooLarge, "{\"errors\":[{\"message\":\"File size limit exceeded.\",\"extensions\":{\"code\":\"FILE_SIZE_EXCEEDED\",\"codes\":[\"FILE_SIZE_EXCEEDED\"]}}]}")]
    [Theory]
    public async Task FormMultipart_Upload_Validation(int? maxFileCount, int? maxFileLength, HttpStatusCode expectedStatusCode, string expectedResponse)
    {
        var operations = "{\"query\":\"query($arg1:FormFile,$arg2:FormFile){file0:file(file:$arg1){content},file1:file(file:$arg2){content}}\",\"variables\":{\"arg1\":null,\"arg2\":null}}";
        var map = "{\"file0\":[\"0.variables.arg1\"],\"file1\":[\"0.variables.arg2\"]}";
        var client = _server.CreateClient();
        _options2.MaximumFileCount = maxFileCount;
        _options2.MaximumFileSize = maxFileLength;
        var content = new MultipartFormDataContent
        {
            { new StringContent(operations, Encoding.UTF8, "application/json"), "operations" },
            { new StringContent(map, Encoding.UTF8, "application/json"), "map" },
            { new StringContent("test1", Encoding.UTF8, "text/text"), "file0", "example1.txt" },
            { new StringContent("test2", Encoding.UTF8, "text/html"), "file1", "example2.html" }
        };
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync((HttpStatusCode)expectedStatusCode, expectedResponse);
    }

    [Fact]
    public async Task FormUrlEncoded()
    {
        var client = _server.CreateClient();
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string?, string?>("query", "query op1{ext} query op2($test:String!){ext var(test:$test)}"),
            new KeyValuePair<string?, string?>("variables", """{"test":"1"}"""),
            new KeyValuePair<string?, string?>("extensions", """{"test":"2"}"""),
            new KeyValuePair<string?, string?>("operationName", "op2"),
        });
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync("""{"data":{"ext":"2","var":"1"}}""");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FormUrlEncoded_DeserializationError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string?, string?>("query", "{ext}"),
            new KeyValuePair<string?, string?>("variables", "{"),
        });
        using var response = await client.PostAsync("/graphql2", content);
        // always returns BadRequest here
        await response.ShouldBeAsync(true, """{"errors":[{"message":"JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1.","extensions":{"code":"JSON_INVALID","codes":["JSON_INVALID"]}}]}""");
    }

    [Theory]
    [InlineData("application/graphql")]
    [InlineData("APPLICATION/GRAPHQL")]
    public async Task ContentType_GraphQL(string contentType)
    {
        var client = _server.CreateClient();
        var content = new StringContent("{count}");
        content.Headers.ContentType = new(contentType);
        using var response = await client.PostAsync("/graphql", content);
        await response.ShouldBeAsync("""{"data":{"count":0}}""");
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/graphql+json")]
    [InlineData("APPLICATION/JSON")]
    [InlineData("APPLICATION/GRAPHQL+JSON")]
    public async Task ContentType_GraphQLJson(string contentType)
    {
        var client = _server.CreateClient();
        var content = new StringContent("""{"query":"{count}"}""");
        content.Headers.ContentType = new(contentType);
        using var response = await client.PostAsync("/graphql", content);
        await response.ShouldBeAsync("""{"data":{"count":0}}""");
    }

    [Theory]
    [InlineData(false, true, "application/pdf")]
    [InlineData(true, true, "application/pdf")]
    [InlineData(false, false, "multipart-form/data")]
    [InlineData(true, false, "multipart-form/data")]
    [InlineData(false, false, "application/x-www-form-urlencoded")]
    [InlineData(true, false, "application/x-www-form-urlencoded")]
    public async Task UnknownContentType(bool badRequest, bool allowFormBody, string contentType)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        _options.ReadFormOnPost = allowFormBody;
        var client = _server.CreateClient();
        HttpContent content = contentType switch
        {
            "application/pdf" => new StringContent("{count}", null, contentType),
            "multipart-form/data" => new MultipartFormDataContent { { new StringContent("{count}", null, "application/graphql"), "query" } },
            "application/x-www-form-urlencoded" => new FormUrlEncodedContent(new[] { new KeyValuePair<string?, string?>("query", "{count}") }),
            _ => throw new ArgumentOutOfRangeException(nameof(contentType))
        };
        using var response = await client.PostAsync("/graphql", content);
        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        var actual = await response.Content.ReadAsStringAsync();
        if (allowFormBody)
        {
            actual.ShouldBe($@"{{""errors"":[{{""message"":""Invalid \u0027Content-Type\u0027 header: non-supported media type \u0027{content.Headers.ContentType?.ToString().Replace("\"", "\\u0022")}\u0027. Must be \u0027application/json\u0027, \u0027application/graphql\u0027 or a form body."",""extensions"":{{""code"":""INVALID_CONTENT_TYPE"",""codes"":[""INVALID_CONTENT_TYPE""]}}}}]}}");
        }
        else
        {
            actual.ShouldBe($@"{{""errors"":[{{""message"":""Invalid \u0027Content-Type\u0027 header: non-supported media type \u0027{content.Headers.ContentType?.ToString().Replace("\"", "\\u0022")}\u0027. Must be \u0027application/json\u0027 or \u0027application/graphql\u0027."",""extensions"":{{""code"":""INVALID_CONTENT_TYPE"",""codes"":[""INVALID_CONTENT_TYPE""]}}}}]}}");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CannotParseContentType(bool badRequest)
    {
        _options2.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        var content = new StringContent("");
        content.Headers.ContentType = null;
        var response = await client.PostAsync("/graphql2", content);
        // always returns unsupported media type
        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldBe("""{"errors":[{"message":"Invalid \u0027Content-Type\u0027 header: value \u0027\u0027 could not be parsed.","extensions":{"code":"INVALID_CONTENT_TYPE","codes":["INVALID_CONTENT_TYPE"]}}]}""");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WithError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostRequestAsync(new() { Query = "{invalid}" });
        await response.ShouldBeAsync(badRequest, """{"errors":[{"message":"Cannot query field \u0027invalid\u0027 on type \u0027Query\u0027.","locations":[{"line":1,"column":2}],"extensions":{"code":"FIELDS_ON_CORRECT_TYPE","codes":["FIELDS_ON_CORRECT_TYPE"],"number":"5.3.1"}}]}""");
    }

    [Fact]
    public async Task Disabled()
    {
        _options.HandlePost = false;
        using var response = await PostRequestAsync(new() { Query = "{count}" });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryParseError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostRequestAsync(new() { Query = "{" });
        await response.ShouldBeAsync(badRequest, """{"errors":[{"message":"Error parsing query: Expected Name, found EOF; for more information see http://spec.graphql.org/October2021/#Field","locations":[{"line":1,"column":2}],"extensions":{"code":"SYNTAX_ERROR","codes":["SYNTAX_ERROR"]}}]}""");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoQuery(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("{}");
        await response.ShouldBeAsync(badRequest, """{"errors":[{"message":"GraphQL query is missing.","extensions":{"code":"QUERY_MISSING","codes":["QUERY_MISSING"]}}]}""");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NullRequest(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("null");
        await response.ShouldBeAsync(badRequest, """{"errors":[{"message":"GraphQL query is missing.","extensions":{"code":"QUERY_MISSING","codes":["QUERY_MISSING"]}}]}""");
    }

    [Fact]
    public async Task Mutation()
    {
        using var response = await PostRequestAsync(new() { Query = "mutation{clearMessages}" });
        await response.ShouldBeAsync("""{"data":{"clearMessages":0}}""");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscription(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostRequestAsync(new() { Query = "subscription{newMessages{id}}" });
        await response.ShouldBeAsync(HttpStatusCode.MethodNotAllowed, """{"errors":[{"message":"Subscription operations are not supported for POST requests.","locations":[{"line":1,"column":1}],"extensions":{"code":"HTTP_METHOD_VALIDATION","codes":["HTTP_METHOD_VALIDATION"]}}]}""");
    }

    [Fact]
    public async Task WithVariables()
    {
        using var response = await PostRequestAsync("/graphql2", new()
        {
            Query = "query($test:String){var(test:$test)}",
            Variables = new Inputs(new Dictionary<string, object?> {
                { "test", "abc" }
            }),
        });
        await response.ShouldBeAsync("""{"data":{"var":"abc"}}""");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParseError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("/graphql2", "{");
        // always returns BadRequest here
        await response.ShouldBeAsync(true, """{"errors":[{"message":"JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1.","extensions":{"code":"JSON_INVALID","codes":["JSON_INVALID"]}}]}""");
    }

    [Theory]
    [InlineData("test1", """{"data":{"count":0}}""")]
    [InlineData("test2", """{"data":{"allMessages":[]}}""")]
    public async Task OperationName(string opName, string expected)
    {
        using var response = await PostRequestAsync(new() { Query = "query test1{count} query test2{allMessages{id}}", OperationName = opName });
        await response.ShouldBeAsync(expected);
    }

    [Fact]
    public async Task Extensions()
    {
        using var response = await PostJsonAsync("/graphql2", """{"query":"{ext}","extensions":{"test":"abc"}}""");
        await response.ShouldBeAsync("""{"data":{"ext":"abc"}}""");
    }

    [Theory]
    [InlineData(false, false, false, """{"data":{"ext":"postext","var":"postvar"}}""")]
    [InlineData(true, false, false, """{"data":{"var":"postvar","altext":"postext"}}""")]
    [InlineData(true, true, false, """{"data":{"var":"urlvar","altext":"postext"}}""")]
    [InlineData(true, false, true, """{"data":{"var":"postvar","altext":"urlext"}}""")]
    [InlineData(true, true, true, """{"data":{"var":"urlvar","altext":"urlext"}}""")]
    public async Task ReadAlsoFromQueryString(bool readFromQueryString, bool readVariablesFromQueryString, bool readExtensionsFromQueryString, string expected)
    {
        _options2.ReadQueryStringOnPost = readFromQueryString;
        _options2.ReadVariablesFromQueryString = readVariablesFromQueryString;
        _options2.ReadExtensionsFromQueryString = readExtensionsFromQueryString;
        var url = "/graphql2?query=query op1($test:String!){altext:ext var(test:$test)} query op2($test:String!){var(test:$test) altext:ext}&operationName=op2&variables={%22test%22:%22urlvar%22}&extensions={%22test%22:%22urlext%22}";
        var request = new GraphQLRequest
        {
            Query = "query op1($test:String!){ext var(test:$test)} query op2($test:String!){var(test:$test) ext}",
            Variables = new Inputs(new Dictionary<string, object?> {
                { "test", "postvar" }
            }),
            Extensions = new Inputs(new Dictionary<string, object?> {
                { "test", "postext" }
            }),
            OperationName = "op1",
        };
        using var response = await PostRequestAsync(url, request);
        await response.ShouldBeAsync(expected);
    }

}

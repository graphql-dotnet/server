using BenchmarkDotNet.Attributes;
using GraphQL.Introspection;
using GraphQL.Server.Common;
using Microsoft.AspNetCore.Http;
using GraphQL.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NsjDeserializer = GraphQL.Server.Transports.AspNetCore.NewtonsoftJson.GraphQLRequestDeserializer;
using StjDeserializer = GraphQL.Server.Transports.AspNetCore.SystemTextJson.GraphQLRequestDeserializer;
using GraphQL.Server.Transports.AspNetCore.Common;
using Microsoft.AspNetCore.Http.Features;

namespace GraphQL.Server.Benchmarks
{
    [MemoryDiagnoser]
    [RPlotExporter, CsvMeasurementsExporter]
    public class DeserializeFromJsonBodyBenchmark
    {
        private NsjDeserializer _nsjDeserializer;
        private StjDeserializer _stjDeserializer;
        private HttpRequest _httpRequest;

        [GlobalSetup]
        public void GlobalSetup()
        //public DeserializeFromJsonBodyBenchmark()
        {
            _nsjDeserializer = new NsjDeserializer(s => { });
            _stjDeserializer = new StjDeserializer(s => { });

            var gqlRequest = new GraphQLRequest { Query = SchemaIntrospection.IntrospectionQuery };
            var gqlRequestJson = Serializer.ToJson(gqlRequest);
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IRequestBodyPipeFeature>(new RequestBodyPipeFeature(httpContext));
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(gqlRequestJson));
            _httpRequest = httpContext.Request;
        }

        [Benchmark(Baseline = true)]
        public Task<GraphQLRequestDeserializationResult> NewtonsoftJson() => _nsjDeserializer.DeserializeFromJsonBodyAsync(_httpRequest);

        [Benchmark]
        public Task<GraphQLRequestDeserializationResult> SystemTextJson() => _stjDeserializer.DeserializeFromJsonBodyAsync(_httpRequest);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Transports.AspNetCore.Abstractions;
using GraphQL.Transports.AspNetCore.Requests;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GraphQL.Transports.AspNetCore
{
    public class HttpRequestTransport<TSchema> : ITransport<TSchema> where TSchema : Schema
    {
        public HttpRequestTransport()
        {
            
        }


        /// <inheritdoc />
        public bool Accepts(HttpContext context)
        {
            if (context.Request.ContentType == null)
                return false;

            if (!context.Request.ContentType?.StartsWith("application/json") == true)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task OnConnectedAsync(HttpContext context)
        {
            var documentExecuter = context.RequestServices.GetRequiredService<IDocumentExecuter>();
            var documentWriter = context.RequestServices.GetRequiredService<IDocumentWriter>();
            var query = await GetQueryAsync(context);
            var result = await documentExecuter.ExecuteAsync(new ExecutionOptions
            {
                Schema = context.RequestServices.GetRequiredService<TSchema>(),
                Query = query.Query,
                OperationName = query.OperationName,
                Inputs = query.GetInputs()
            });

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await WriteResponseJson(context.Response.Body, result, documentWriter);
        }

        private static async Task WriteResponseJson(Stream responseBody, ExecutionResult result, IDocumentWriter documentWriter)
        {
            var json = documentWriter.Write(result);

            using (var streamWriter = new StreamWriter(responseBody, Encoding.UTF8, 4069, true))
            {
                await streamWriter.WriteAsync(json);
                await streamWriter.FlushAsync();
            }
        }

        private static async Task<GraphQuery> GetQueryAsync(HttpContext context)
        {
            using (var reader = new StreamReader(context.Request.Body))
            {
                var json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<GraphQuery>(json);
            }
        }
    }
}

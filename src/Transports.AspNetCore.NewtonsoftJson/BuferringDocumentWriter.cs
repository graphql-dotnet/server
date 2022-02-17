using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.NewtonsoftJson;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    public sealed class BufferingDocumentWriter : IDocumentWriter
    {
        private readonly DocumentWriter _documentWriter;

        public BufferingDocumentWriter(Action<JsonSerializerSettings> action, IErrorInfoProvider errorInfoProvider)
        {
            _documentWriter = new DocumentWriter(action, errorInfoProvider);
        }

        public async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            await using (var bufferStream = new FileBufferingWriteStream())
            {
                await _documentWriter.WriteAsync(bufferStream, value, cancellationToken);

                await bufferStream.DrainBufferAsync(stream, cancellationToken);
            }
        }
    }
}

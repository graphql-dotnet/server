using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Server.Transports.Subscriptions.Abstractions;

namespace GraphQL.Server
{
    /// <summary>
    /// OperationMessage uses DataMemberAttribute but System.Text.Json does not support this:
    /// https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to#systemruntimeserialization-attributes
    ///
    /// TODO: it may be worth refactor since a reference to Transports.Subscriptions.Abstractions is required now
    /// </summary>
    internal sealed class OperationMessageConverter : JsonConverter<OperationMessage>
    {
        public override OperationMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, OperationMessage message, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteStringValue(message.Id);

            writer.WritePropertyName("type");
            writer.WriteStringValue(message.Type);

            writer.WritePropertyName("payload");
            JsonSerializer.Serialize(writer, message.Payload, options);

            writer.WriteEndObject();
        }
    }
}

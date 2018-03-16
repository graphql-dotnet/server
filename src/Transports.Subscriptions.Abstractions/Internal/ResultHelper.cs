using System.Text;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Internal
{
    internal class ResultHelper
    {
        public static string GetErrorString(ExecutionResult result)
        {
            if (result.Errors == null)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var error in result.Errors)
            {
                builder.AppendLine($"Error: {error.Message}");

                if (error.Path != null) builder.AppendLine($"Path: {string.Join(".", error.Path)}");

                builder.AppendLine($"Locations:");
                if (error.Locations != null)
                    foreach (var location in error.Locations)
                        builder.AppendLine($"Line: {location.Line} Column: {location.Column}");
            }

            return builder.ToString();
        }
    }
}
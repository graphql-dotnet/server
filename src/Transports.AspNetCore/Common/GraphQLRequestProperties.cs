using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public static class GraphQLRequestProperties
    {
        public const string QueryKey = "query";
        public const string VariablesKey = "variables";
        public const string OperationNameKey = "operationName";
    }
}

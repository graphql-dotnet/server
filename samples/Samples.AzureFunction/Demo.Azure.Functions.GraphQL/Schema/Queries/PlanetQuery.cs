using System;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using GraphQL.Types;
using Demo.Azure.Functions.GraphQL.Documents;
using Demo.Azure.Functions.GraphQL.Schema.Types;
using Demo.Azure.Functions.GraphQL.Infrastructure;

namespace Demo.Azure.Functions.GraphQL.Schema.Queries
{
    internal class PlanetQuery: ObjectGraphType
    {
        private static readonly Uri _planetsCollectionUri = UriFactory.CreateDocumentCollectionUri(Constants.DATABASE_NAME, Constants.PLANETS_COLLECTION_NAME);
        private static readonly FeedOptions _feedOptions = new FeedOptions { MaxItemCount = -1, };

        public PlanetQuery(IDocumentClient documentClient)
        {
            Field<ListGraphType<PlanetType>>(
                "planets",
                resolve: context => documentClient.CreateDocumentQuery<Planet>(_planetsCollectionUri, _feedOptions)
            );
        }
    }
}

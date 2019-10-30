using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using GraphQL.Types;
using GraphQL.DataLoader;
using Demo.Azure.Functions.GraphQL.Documents;
using Demo.Azure.Functions.GraphQL.Infrastructure;

namespace Demo.Azure.Functions.GraphQL.Schema.Types
{
    internal class PlanetType: ObjectGraphType<Planet>
    {
        private static readonly Uri _planetsCollectionUri = UriFactory.CreateDocumentCollectionUri(Constants.DATABASE_NAME, Constants.CHARACTERS_COLLECTION_NAME);
        private static readonly FeedOptions _feedOptions = new FeedOptions { MaxItemCount = -1, };

        private readonly IDocumentClient _documentClient;

        public PlanetType(IDocumentClient documentClient, IDataLoaderContextAccessor dataLoaderContextAccessor)
        {
            _documentClient = documentClient;

            Field(t => t.WorldId);
            Field(t => t.Name);

            Field<ListGraphType<CharacterType>>(
                "characters",
                resolve: context => {
                    var dataLoader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<int, Character>("GetCharactersByHomeworldId", GetCharactersByHomeworldId);

                    return dataLoader.LoadAsync(context.Source.WorldId);
                }
            );
        }

        private Task<ILookup<int, Character>> GetCharactersByHomeworldId(IEnumerable<int> homeworldIds)
        {
            return Task.FromResult(_documentClient.CreateDocumentQuery<Character>(_planetsCollectionUri, _feedOptions)
                .Where(c => homeworldIds.Contains(c.HomeworldId))
                .ToLookup(c => c.HomeworldId)
            );
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Demo.Azure.Functions.GraphQL.Documents;
using Demo.Azure.Functions.GraphQL.Infrastructure;

namespace Demo.Azure.Functions.GraphQL
{
    public static class SeedFunction
    {
        [FunctionName("seed")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request,
            [CosmosDB(databaseName: Constants.DATABASE_NAME, collectionName: Constants.PLANETS_COLLECTION_NAME, ConnectionStringSetting = Constants.CONNECTION_STRING_SETTING, CreateIfNotExists = true)] IAsyncCollector<Planet> planetsCollector,
            [CosmosDB(databaseName: Constants.DATABASE_NAME, collectionName: Constants.CHARACTERS_COLLECTION_NAME, ConnectionStringSetting = Constants.CONNECTION_STRING_SETTING, CreateIfNotExists = true)] IAsyncCollector<Character> charactersCollector)
        {
            await planetsCollector.AddAsync(new Planet { WorldId = 1, Name = "Tatooine" });
            await planetsCollector.AddAsync(new Planet { WorldId = 2, Name = "Alderaan" });
            await planetsCollector.AddAsync(new Planet { WorldId = 8, Name = "Naboo" });
            await planetsCollector.AddAsync(new Planet { WorldId = 10, Name = "Kamino" });
            await planetsCollector.AddAsync(new Planet { WorldId = 14, Name = "Kashyyyk" });
            await planetsCollector.AddAsync(new Planet { WorldId = 20, Name = "Stewjon" });
            await planetsCollector.AddAsync(new Planet { WorldId = 21, Name = "Eriadu" });
            await planetsCollector.AddAsync(new Planet { WorldId = 22, Name = "Corellia" });
            await planetsCollector.AddAsync(new Planet { WorldId = 23, Name = "Rodia" });
            await planetsCollector.AddAsync(new Planet { WorldId = 24, Name = "Nal Hutta" });
            await planetsCollector.AddAsync(new Planet { WorldId = 26, Name = "Bestine IV" });

            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Luke Skywalker", BirthYear = "19BBY", HomeworldId = 1 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "C-3PO", BirthYear = "112BBY", HomeworldId = 1 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "R2-D2", BirthYear = "33BBY", HomeworldId = 8 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Darth Vader", BirthYear = "41.9BBY", HomeworldId = 1 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Leia Organa", BirthYear = "19BBY", HomeworldId = 2 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Owen Lars", BirthYear = "52BBY", HomeworldId = 1 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Beru Whitesun Lars", BirthYear = "47BBY", HomeworldId = 1 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "R5-D4", BirthYear = "Unknown", HomeworldId = 1 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Biggs Darklighter", BirthYear = "24BBY", HomeworldId = 1 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Obi-Wan Kenobi", BirthYear = "57BBY", HomeworldId = 20 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Anakin Skywalker", BirthYear = "41.9BBY", HomeworldId = 1 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Wilhuff Tarkin", BirthYear = "64BBY", HomeworldId = 21 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Chewbacca", BirthYear = "200BBY", HomeworldId = 14 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Han Solo", BirthYear = "29BBY", HomeworldId = 22 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Greedo", BirthYear = "44BBY", HomeworldId = 23 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Jabba Desilijic Tiure", BirthYear = "600BBY", HomeworldId = 24 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Wedge Antilles", BirthYear = "21BBY", HomeworldId = 22 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Jek Tono Porkins", BirthYear = "Unknown", HomeworldId = 26 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Palpatine", BirthYear = "82BBY", HomeworldId = 8 });
            await charactersCollector.AddAsync(new Character { CharacterId = Guid.NewGuid().ToString("N"), Name = "Boba Fett", BirthYear = "31.5BBY", HomeworldId = 10 });

            return new OkResult();
        }
    }
}

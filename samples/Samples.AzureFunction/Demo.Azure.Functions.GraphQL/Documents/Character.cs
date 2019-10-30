using Newtonsoft.Json;

namespace Demo.Azure.Functions.GraphQL.Documents
{
    public class Character
    {
        public string CharacterId { get; set; }

        public string Name { get; set; }

        public int HomeworldId { get; set; }

        public string BirthYear { get; set; }
    }
}

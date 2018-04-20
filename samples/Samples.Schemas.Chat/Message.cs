using System;

namespace GraphQL.Samples.Schemas.Chat
{
    public class Message
    {
        public MessageFrom From { get; set; }

        public string Sub { get; set; }

        public string Content { get; set; }

        public DateTime SentAt { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Server.Transports.WebSockets.Abstractions
{
    public interface ISubscriptionDeterminator
    {
        bool IsSubscription(ExecutionOptions config);
    }
}

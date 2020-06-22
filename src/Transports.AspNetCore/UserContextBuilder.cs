using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class UserContextBuilder<TUserContext> : IUserContextBuilder
        where TUserContext : IDictionary<string, object>
    {
        private readonly Func<HttpContext, Task<TUserContext>> _func;

        public UserContextBuilder(Func<HttpContext, Task<TUserContext>> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public UserContextBuilder(Func<HttpContext, TUserContext> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            _func = x => Task.FromResult(func(x));
        }

        public async Task<IDictionary<string, object>> BuildUserContext(HttpContext httpContext) => await _func(httpContext);
    }
}

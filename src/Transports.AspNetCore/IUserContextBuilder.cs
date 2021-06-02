using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore
{
    /// <summary>
    /// Interface which is responsible of building a UserContext for a GraphQL request
    /// </summary>
    public interface IUserContextBuilder
    {
        /// <summary>
        /// Builds the UserContext using the specified <see cref="HttpContext"/>
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current request</param>
        /// <returns>Returns the UserContext</returns>
        Task<IDictionary<string, object>> BuildUserContext(HttpContext httpContext);
    }
}

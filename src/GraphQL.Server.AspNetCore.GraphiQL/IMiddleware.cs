using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.AspNetCore.GraphiQL {

	/// <summary>
	/// Represents a Middleware for AspNetCore
	/// </summary>
	public interface IMiddleware {

		/// <summary>
		/// Invoke the action of a Middleware
		/// </summary>
		/// <param name="httpContext"></param>
		/// <returns></returns>
		Task Invoke(HttpContext httpContext);

	}

	/// <summary>
	/// Base Class for implement Middlewares
	/// </summary>
	public abstract class BaseMiddleware : IMiddleware {

		/// <summary>
		/// The Next Middleware
		/// </summary>
		protected RequestDelegate NextMiddleware { get; }

		/// <summary>
		/// Initialize an instance of a Middleware
		/// </summary>
		/// <param name="nextMiddleware"></param>
		public BaseMiddleware(RequestDelegate nextMiddleware) {
			this.NextMiddleware = nextMiddleware ?? throw new ArgumentNullException(nameof(nextMiddleware));
		}

		/// <summary>
		/// Invoke the action of a Middleware
		/// </summary>
		/// <param name="httpContext"></param>
		/// <returns></returns>
		public abstract Task Invoke(HttpContext httpContext);

	}

}

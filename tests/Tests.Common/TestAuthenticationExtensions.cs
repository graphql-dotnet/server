using System;
using System.Net.Http;
using GraphQL.Server.Tests.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server
{
    public static class TestAuthenticationExtensions
    {
        public static AuthenticationBuilder AddTestAuthentication(this AuthenticationBuilder authentication)
        {
            return authentication.AddTestAuthentication(_ => { });
        }

        public static AuthenticationBuilder AddTestAuthentication(this AuthenticationBuilder authentication, Action<TestAuthenticationOptions> configure)
        {
            authentication.AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                TestAuthenticationDefaults.AuthenticationScheme, configure);

            return authentication;
        }

        /// <summary>
        /// Add a claim header to the request that will be processed by the TestAuthenticationHandler
        /// </summary>
        /// <param name="request"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HttpRequest AddClaimHeader(this HttpRequest request, string type, string value)
        {
            request.Headers.Add(TestAuthenticationDefaults.ClaimHeaderPrefix + type, value);
            return request;
        }

        /// <summary>
        /// Add a claim header to the client that will be processed by the TestAuthenticationHandler
        /// </summary>
        /// <param name="client"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HttpClient AddClaimHeader(this HttpClient client, string type, string value)
        {
            client.DefaultRequestHeaders.Add(TestAuthenticationDefaults.ClaimHeaderPrefix + type, value);
            return client;
        }
    }
}

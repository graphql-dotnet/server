using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace GraphQL.Server.Transports.AspNetCore.Tests
{
    public class AuthenticatedGraphQLFacts
    {
        private readonly TestServer _server;

        public AuthenticatedGraphQLFacts()
        {
            _server = new TestServer(WebHost.CreateDefaultBuilder()
                .UseStartup<AuthenticatedTestStartup>());
        }

        [Fact]
        public async Task unauthenticated_request_should_return_401()
        {
            /* Given */
            var client = _server.CreateClient();

            /* When */
            var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await client.QueryAsync(IntrospectionQuery);
            });

            /* Then */
            Assert.Contains("401", ex.Message);
        }

        [Fact]
        public async Task authorized_request_should_succeed()
        {
            /* Given */
            var client = _server.CreateClient();
            client.AddClaimHeader("sub", "25");
            client.AddClaimHeader("role", "admin");

            /* When */
            var response = await client.QueryAsync(IntrospectionQuery);

            /* Then */
            Assert.NotNull(response);
        }

        [Fact]
        public async Task unauthorized_request_should_return_403()
        {
            /* Given */
            var client = _server.CreateClient();
            client.AddClaimHeader("sub", "23");
            client.AddClaimHeader("role", "user");

            /* When */
            var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await client.QueryAsync(IntrospectionQuery);
            });

            /* Then */
            Assert.Contains("403", ex.Message);
        }

        private const string IntrospectionQuery = @"
				query IntrospectionQuery {
					__schema {
						queryType {
							name
						},
						mutationType {
							name
						},
						subscriptionType {
							name
						},
						types {
							...FullType
						},
						directives {
							name,
							description,
							args {
								...InputValue
							},
							onOperation,
							onFragment,
							onField
						}
					}
				}
				fragment FullType on __Type {
					kind,
					name,
					description,
					fields(includeDeprecated: true) {
						name,
						description,
						args {
							...InputValue
						},
						type {
							...TypeRef
						},
						isDeprecated,
						deprecationReason
					},
					inputFields {
						...InputValue
					},
					interfaces {
						...TypeRef
					},
					enumValues(includeDeprecated: true) {
						name,
						description,
						isDeprecated,
						deprecationReason
					},
					possibleTypes {
						...TypeRef
					}
				}
				fragment InputValue on __InputValue {
					name,
					description,
					type {
						...TypeRef
					},
					defaultValue
				}
				fragment TypeRef on __Type {
					kind,
					name,
					ofType {
						kind,
						name,
						ofType {
							kind,
							name,
							ofType {
								kind,
								name
							}
						}
					}
				}";

    }
}

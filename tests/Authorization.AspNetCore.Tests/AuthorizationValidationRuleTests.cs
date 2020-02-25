using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using System.Collections.Generic;
using Xunit;

namespace GraphQL.Server.Authorization.AspNetCore.Tests
{
    public class AuthorizationValidationRuleTests : ValidationTestBase
    {
        [Fact]
        public void class_policy_success()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("ClassPolicy", x => x.RequireClaim("admin")));

            ShouldPassRule(_ =>
            {
                _.Query = @"query { post }";
                _.Schema = BasicSchema<BasicQueryWithAttributesAndClassPolicy>();
                _.User = CreatePrincipal(claims: new Dictionary<string, string>
                    {
                        { "Admin", "true" }
                    });
            });
        }

        [Fact]
        public void class_policy_fail()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("ClassPolicy", x => x.RequireClaim("admin")));

            ShouldFailRule(_ =>
            {
                _.Query = @"query { post }";
                _.Schema = BasicSchema<BasicQueryWithAttributesAndClassPolicy>();
            });
        }

        [Fact]
        public void field_policy_success()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

            ShouldPassRule(_ =>
            {
                _.Query = @"query { post }";
                _.Schema = BasicSchema<BasicQueryWithAttributesAndFieldPolicy>();
                _.User = CreatePrincipal(claims: new Dictionary<string, string>
                    {
                        { "Admin", "true" }
                    });
            });
        }

        [Fact]
        public void field_policy_fail()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

            ShouldFailRule(_ =>
            {
                _.Query = @"query { post }";
                _.Schema = BasicSchema<BasicQueryWithAttributesAndFieldPolicy>();
            });
        }

        [Fact]
        public void nested_type_policy_success()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("PostPolicy", x => x.RequireClaim("admin")));

            ShouldPassRule(_ =>
            {
                _.Query = @"query { post }";
                _.Schema = NestedSchema();
                _.User = CreatePrincipal(claims: new Dictionary<string, string>
                    {
                        { "Admin", "true" }
                    });
            });
        }

        [Fact]
        public void nested_type_policy_fail()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("PostPolicy", x => x.RequireClaim("admin")));

            ShouldFailRule(_ =>
            {
                _.Query = @"query { post }";
                _.Schema = NestedSchema();
            });
        }

        [Fact]
        public void passes_with_claim_on_input_type()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

            ShouldPassRule(_ =>
            {
                _.Query = @"query { author(input: { name: ""Quinn"" }) }";
                _.Schema = TypedSchema();
                _.User = CreatePrincipal(claims: new Dictionary<string, string>
                    {
                        { "Admin", "true" }
                    });
            });
        }

        [Fact]
        public void nested_type_list_policy_fail()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("PostPolicy", x => x.RequireClaim("admin")));

            ShouldFailRule(_ =>
            {
                _.Query = @"query { posts }";
                _.Schema = NestedSchema();
            });
        }

        [Fact]
        public void nested_type_list_non_null_policy_fail()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("PostPolicy", x => x.RequireClaim("admin")));

            ShouldFailRule(_ =>
            {
                _.Query = @"query { postsNonNull }";
                _.Schema = NestedSchema();
            });
        }

        [Fact]
        public void fails_on_missing_claim_on_input_type()
        {
            ConfigureAuthorizationOptions(
                options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

            ShouldFailRule(_ =>
            {
                _.Query = @"query { author(input: { name: ""Quinn"" }) }";
                _.Schema = TypedSchema();
            });
        }

        [Fact]
        public void passes_with_policy_on_connection_type()
        {
            ConfigureAuthorizationOptions(options => options.AddPolicy("ConnectionPolicy", x => x.RequireClaim("admin")));

            ShouldPassRule(_ =>
            {
                _.Query = @"query { posts { items { id } } }";
                _.Schema = TypedSchema();
                _.User = CreatePrincipal(claims: new Dictionary<string, string>
                {
                    { "Admin", "true" }
                });
            });
        }

        [Fact]
        public void fails_on_missing_claim_on_connection_type()
        {
            ConfigureAuthorizationOptions(options => options.AddPolicy("ConnectionPolicy", x => x.RequireClaim("admin")));

            ShouldFailRule(_ =>
            {
                _.Query = @"query { posts { items { id } } }";
                _.Schema = TypedSchema();
                _.User = CreatePrincipal();
            });
        }

        private ISchema BasicSchema<T>()
        {
            string defs = @"
                type Query {
                    post(id: ID!): String
                }
            ";

            return Schema.For(defs, _ => _.Types.Include<T>());
        }

        [GraphQLMetadata("Query")]
        [GraphQLAuthorize(Policy = "ClassPolicy")]
        public class BasicQueryWithAttributesAndClassPolicy
        {
            public string Post(string id) => "";
        }

        [GraphQLMetadata("Query")]
        public class BasicQueryWithAttributesAndFieldPolicy
        {
            [GraphQLAuthorize(Policy = "FieldPolicy")]
            public string Post(string id) => "";
        }

        private ISchema NestedSchema()
        {
            string defs = @"
                type Query {
                    post(id: ID!): Post
                    posts: [Post]
                    postsNonNull: [Post!]!
                }

                type Post {
                    id: ID!
                }
            ";

            return Schema.For(defs, _ =>
            {
                _.Types.Include<NestedQueryWithAttributes>();
                _.Types.Include<Post>();
            });
        }

        [GraphQLMetadata("Query")]
        public class NestedQueryWithAttributes
        {
            public Post Post(string id) => null;

            public IEnumerable<Post> Posts() => null;

            public IEnumerable<Post> PostsNonNull() => null;
        }

        [GraphQLAuthorize(Policy = "PostPolicy")]
        public class Post
        {
            public string Id { get; set; }
        }

        public class PostGraphType : ObjectGraphType<Post>
        {
            public PostGraphType()
            {
                Field(p => p.Id);
            }
        }

        public class Author
        {
            public string Name { get; set; }
        }

        private ISchema TypedSchema()
        {
            var query = new ObjectGraphType();
            
            query.Field<StringGraphType>(
                "author",
                arguments: new QueryArguments(new QueryArgument<AuthorInputType> { Name = "input" }),
                resolve: context => "testing"
            );

            query.Connection<PostGraphType>()
                .Name("posts")
                .AuthorizeWith("ConnectionPolicy")
                .Resolve(ctx => new Connection<Post>());

            return new Schema { Query = query };
        }

        public class AuthorInputType : InputObjectGraphType<Author>
        {
            public AuthorInputType()
            {
                Field(x => x.Name).AuthorizeWith("FieldPolicy");
            }
        }
    }
}

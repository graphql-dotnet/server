using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.Server.Authorization.AspNetCore.Tests;

public class AuthorizationValidationRuleTests : ValidationTestBase
{
    // https://github.com/graphql-dotnet/server/issues/463
    [Fact]
    public void policy_on_schema_success()
    {
        ConfigureAuthorizationOptions(options =>
        {
            options.AddPolicy("ClassPolicy", x => x.RequireClaim("admin"));
            options.AddPolicy("SchemaPolicy", x => x.RequireClaim("some"));
        });

        ShouldPassRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = BasicSchema<BasicQueryWithAttributesAndClassPolicy>().AuthorizeWithPolicy("SchemaPolicy");
            config.User = CreatePrincipal(claims: new Dictionary<string, string>
            {
                { "Admin", "true" },
                { "some", "abcdef" }
            });
        });
    }

    // https://github.com/graphql-dotnet/server/issues/463
    [Fact]
    public void policy_on_schema_fail()
    {
        ConfigureAuthorizationOptions(options =>
        {
            options.AddPolicy("ClassPolicy", x => x.RequireClaim("admin"));
            options.AddPolicy("SchemaPolicy", x => x.RequireClaim("some"));
        });

        ShouldFailRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = BasicSchema<BasicQueryWithAttributesAndClassPolicy>().AuthorizeWithPolicy("SchemaPolicy");
            config.User = CreatePrincipal(claims: new Dictionary<string, string>
            {
                { "Admin", "true" },
            });
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'some' is not present.");
            };
        });
    }

    [Fact]
    public void class_policy_success()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("ClassPolicy", x => x.RequireClaim("admin")));

        ShouldPassRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = BasicSchema<BasicQueryWithAttributesAndClassPolicy>();
            config.User = CreatePrincipal(claims: new Dictionary<string, string>
            {
                { "Admin", "true" }
            });
        });
    }

    [Fact]
    public void class_policy_fail()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("ClassPolicy", x => x.RequireClaim("admin")));

        ShouldFailRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = BasicSchema<BasicQueryWithAttributesAndClassPolicy>();
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'admin' is not present.");
            };
        });
    }

    [Fact]
    public void method_policy_success()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

        ShouldPassRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = BasicSchema<BasicQueryWithAttributesAndMethodPolicy>();
            config.User = CreatePrincipal(claims: new Dictionary<string, string>
            {
                { "Admin", "true" }
            });
        });
    }

    [Fact]
    public void property_policy_success()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

        ShouldPassRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = BasicSchema<BasicQueryWithAttributesAndPropertyPolicy>();
            config.User = CreatePrincipal(claims: new Dictionary<string, string>
            {
                { "Admin", "true" }
            });
        });
    }

    [Fact]
    public void method_policy_fail()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

        ShouldFailRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = BasicSchema<BasicQueryWithAttributesAndMethodPolicy>();
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'admin' is not present.");
            };
        });
    }

    [Fact]
    public void property_policy_fail()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

        ShouldFailRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = BasicSchema<BasicQueryWithAttributesAndPropertyPolicy>();
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'admin' is not present.");
            };
        });
    }

    [Fact]
    public void nested_type_policy_success()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("PostPolicy", x => x.RequireClaim("admin")));

        ShouldPassRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = NestedSchema();
            config.User = CreatePrincipal(claims: new Dictionary<string, string>
            {
                { "Admin", "true" }
            });
        });
    }

    [Fact]
    public void nested_type_policy_fail()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("PostPolicy", x => x.RequireClaim("admin")));

        ShouldFailRule(config =>
        {
            config.Query = @"query { post }";
            config.Schema = NestedSchema();
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'admin' is not present.");
            };
        });
    }

    [Fact]
    public void passes_with_claim_on_input_type()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

        ShouldPassRule(config =>
        {
            config.Query = @"query { author(input: { name: ""Quinn"" }) }";
            config.Schema = TypedSchema();
            config.User = CreatePrincipal(claims: new Dictionary<string, string>
            {
                { "Admin", "true" }
            });
        });
    }

    [Fact]
    public void nested_type_list_policy_fail()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("PostPolicy", x => x.RequireClaim("admin")));

        ShouldFailRule(config =>
        {
            config.Query = @"query { posts }";
            config.Schema = NestedSchema();
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'admin' is not present.");
            };
        });
    }

    [Fact]
    public void nested_type_list_non_null_policy_fail()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("PostPolicy", x => x.RequireClaim("admin")));

        ShouldFailRule(config =>
        {
            config.Query = @"query { postsNonNull }";
            config.Schema = NestedSchema();
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'admin' is not present.");
            };
        });
    }

    [Fact(Skip = "Not supported in v7")]
    public void fails_on_missing_claim_on_input_type()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("FieldPolicy", x => x.RequireClaim("admin")));

        ShouldFailRule(config =>
        {
            config.Query = @"query { author(input: { name: ""Quinn"" }) }";
            config.Schema = TypedSchema();
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'admin' is not present.");
            };
        });
    }

    [Fact]
    public void passes_with_policy_on_connection_type()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("ConnectionPolicy", x => x.RequireClaim("admin")));

        ShouldPassRule(config =>
        {
            config.Query = @"query { posts { items { id } } }";
            config.Schema = TypedSchema();
            config.User = CreatePrincipal(claims: new Dictionary<string, string>
            {
                { "Admin", "true" }
            });
        });
    }

    [Fact]
    public void fails_on_missing_claim_on_connection_type()
    {
        ConfigureAuthorizationOptions(options => options.AddPolicy("ConnectionPolicy", x => x.RequireClaim("admin")));

        ShouldFailRule(config =>
        {
            config.Query = @"query { posts { items { id } } }";
            config.Schema = TypedSchema();
            config.User = CreatePrincipal();
            config.ValidateResult = result =>
            {
                result.Errors.Count.ShouldBe(1);
                result.Errors[0].Message.ShouldBe(@"You are not authorized to run this query.
Required claim 'admin' is not present.");
            };
        });
    }

    private static ISchema BasicSchema<T>()
    {
        string defs = @"
                type Query {
                    post(id: ID!): String
                }
            ";

        return Schema.For(defs, _ => _.Types.Include<T>());
    }

    [GraphQLMetadata("Query")]
    [Authorize("ClassPolicy")]
    public class BasicQueryWithAttributesAndClassPolicy
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
        public string Post(string id) => "";
    }

    [GraphQLMetadata("Query")]
    public class BasicQueryWithAttributesAndMethodPolicy
    {
        [Authorize("FieldPolicy")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
        public string Post(string id) => "";
    }

    [GraphQLMetadata("Query")]
    public class BasicQueryWithAttributesAndPropertyPolicy
    {
        [Authorize("FieldPolicy")]
        public string Post { get; set; } = "";
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
        public Post Post(string id) => null;

        public IEnumerable<Post> Posts() => null;

        public IEnumerable<Post> PostsNonNull() => null;
    }

    [Authorize("PostPolicy")]
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
            .AuthorizeWithPolicy("ConnectionPolicy")
            .Resolve(ctx => new Connection<Post>());

        return new Schema { Query = query };
    }

    public class AuthorInputType : InputObjectGraphType<Author>
    {
        public AuthorInputType()
        {
            Field(x => x.Name).AuthorizeWithPolicy("FieldPolicy");
        }
    }
}

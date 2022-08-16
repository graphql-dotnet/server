using System.Security.Claims;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Authorization;

namespace Tests;

public class AuthorizationTests
{
    private readonly Schema _schema = new();
    private readonly ObjectGraphType _query = new() { Name = "QueryType" };
    private readonly FieldType _field = new() { Name = "parent" };
    private readonly ObjectGraphType _childGraph = new() { Name = "ChildType" };
    private readonly FieldType _childField = new() { Name = "child" };
    private readonly QueryArgument _argument = new(typeof(StringGraphType)) { Name = "Arg" };
    private readonly QueryArguments _arguments = new();
    private ClaimsPrincipal _principal = new(new ClaimsIdentity());
    private bool _policyPasses;

    public AuthorizationTests()
    {
        _arguments.Add(_argument);
        _childField.Arguments = _arguments;
        _childField.Type = typeof(StringGraphType);
        _childGraph.AddField(_childField);
        _field.ResolvedType = _childGraph;
        _query.AddField(_field);
        _schema.Query = _query;
    }

    private void SetAuthorized()
    {
        // set principal to an authenticated user in the role "MyRole"
        _principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Role, "MyRole") }, "Cookie"));
        _policyPasses = true;
    }

    private IValidationResult Validate(string query, bool shouldPassCoreRules = true, string? variables = null)
    {
        var mockAuthorizationService = new Mock<IAuthorizationService>(MockBehavior.Strict);
        mockAuthorizationService.Setup(x => x.AuthorizeAsync(_principal, null, It.IsAny<string>())).Returns<ClaimsPrincipal, object, string>((_, _, policy) =>
        {
            if (policy == "MyPolicy" && _policyPasses)
                return Task.FromResult(AuthorizationResult.Success());
            return Task.FromResult(AuthorizationResult.Failed());
        });
        var mockServices = new Mock<IServiceProvider>(MockBehavior.Strict);
        mockServices.Setup(x => x.GetService(typeof(IAuthorizationService))).Returns(mockAuthorizationService.Object);
        var document = GraphQLParser.Parser.Parse(query);

        var inputs = new GraphQLSerializer().Deserialize<Inputs>(variables) ?? Inputs.Empty;

        var validator = new DocumentValidator();
        var (coreRulesResult, _) = validator.ValidateAsync(new ValidationOptions
        {
            Document = document,
            Extensions = Inputs.Empty,
            Operation = (GraphQLOperationDefinition)document.Definitions.First(x => x.Kind == ASTNodeKind.OperationDefinition),
            Schema = _schema,
            UserContext = new Dictionary<string, object?>(),
            Variables = inputs,
            RequestServices = mockServices.Object,
            User = _principal,
        }).GetAwaiter().GetResult(); // there is no async code being tested
        coreRulesResult.IsValid.ShouldBe(shouldPassCoreRules);

        var (result, _) = validator.ValidateAsync(new ValidationOptions
        {
            Document = document,
            Extensions = Inputs.Empty,
            Operation = (GraphQLOperationDefinition)document.Definitions.First(x => x.Kind == ASTNodeKind.OperationDefinition),
            Rules = new IValidationRule[] { new AuthorizationValidationRule() },
            Schema = _schema,
            UserContext = new Dictionary<string, object?>(),
            Variables = inputs,
            RequestServices = mockServices.Object,
            User = _principal,
        }).GetAwaiter().GetResult(); // there is no async code being tested
        return result;
    }

    [Fact]
    public void Simple()
    {
        var ret = Validate(@"{ parent { child(arg: null) } }");
        ret.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ErrorHasPolicy()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));
        Apply(_query, Mode.PolicyFailure);
        var ret = Validate(@"{ parent { child } }");
        var err = ret.Errors.ShouldHaveSingleItem().ShouldBeOfType<AccessDeniedError>();
        err.PolicyRequired.ShouldBe("FailingPolicy");
        err.PolicyAuthorizationResult.ShouldNotBeNull();
        err.PolicyAuthorizationResult.Succeeded.ShouldBeFalse();
        err.RolesRequired.ShouldBeNull();
    }

    [Fact]
    public void ErrorHasRole()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));
        Apply(_query, Mode.RoleFailure);
        var ret = Validate(@"{ parent { child } }");
        var err = ret.Errors.ShouldHaveSingleItem().ShouldBeOfType<AccessDeniedError>();
        err.RolesRequired.ShouldBe(new string[] { "FailingRole" });
        err.PolicyAuthorizationResult.ShouldBeNull();
        err.PolicyRequired.ShouldBeNull();
    }

    [Fact]
    public void ErrorHasRoles()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));
        _query.AuthorizeWithRoles("Role1,Role2");
        var ret = Validate(@"{ parent { child } }");
        var err = ret.Errors.ShouldHaveSingleItem().ShouldBeOfType<AccessDeniedError>();
        err.RolesRequired.ShouldBe(new string[] { "Role1", "Role2" });
        err.PolicyAuthorizationResult.ShouldBeNull();
        err.PolicyRequired.ShouldBeNull();
    }

    [Theory]
    [InlineData("{ parent @skip(if: true) { child } test }", null, false, true)]
    [InlineData("{ parent @skip(if: false) { child } test }", null, false, false)]
    [InlineData("{ parent @skip(if: true) { child } test }", null, true, true)]
    [InlineData("{ parent @skip(if: false) { child } test }", null, true, true)]
    [InlineData("{ parent @include(if: true) { child } test }", null, false, false)]
    [InlineData("{ parent @include(if: false) { child } test }", null, false, true)]
    [InlineData("{ parent @include(if: true) { child } test }", null, true, true)]
    [InlineData("{ parent @include(if: false) { child } test }", null, true, true)]

    [InlineData("query ($arg: Boolean = true) { parent @skip(if: $arg) { child } test }", null, false, true)]
    [InlineData("query ($arg: Boolean = false) { parent @skip(if: $arg) { child } test }", null, false, false)]
    [InlineData("query ($arg: Boolean = true) { parent @skip(if: $arg) { child } test }", null, true, true)]
    [InlineData("query ($arg: Boolean = false) { parent @skip(if: $arg) { child } test }", null, true, true)]
    [InlineData("query ($arg: Boolean = true) { parent @include(if: $arg) { child } test }", null, false, false)]
    [InlineData("query ($arg: Boolean = false) { parent @include(if: $arg) { child } test }", null, false, true)]
    [InlineData("query ($arg: Boolean = true) { parent @include(if: $arg) { child } test }", null, true, true)]
    [InlineData("query ($arg: Boolean = false) { parent @include(if: $arg) { child } test }", null, true, true)]

    [InlineData("query ($arg: Boolean = false) { parent @skip(if: $arg) { child } test }", @"{ ""arg"": true }", false, true)]
    [InlineData("query ($arg: Boolean = true) { parent @skip(if: $arg) { child } test }", @"{ ""arg"": false }", false, false)]
    [InlineData("query ($arg: Boolean = false) { parent @skip(if: $arg) { child } test }", @"{ ""arg"": true }", true, true)]
    [InlineData("query ($arg: Boolean = true) { parent @skip(if: $arg) { child } test }", @"{ ""arg"": false }", true, true)]
    [InlineData("query ($arg: Boolean = false) { parent @include(if: $arg) { child } test }", @"{ ""arg"": true }", false, false)]
    [InlineData("query ($arg: Boolean = true) { parent @include(if: $arg) { child } test }", @"{ ""arg"": false }", false, true)]
    [InlineData("query ($arg: Boolean = false) { parent @include(if: $arg) { child } test }", @"{ ""arg"": true }", true, true)]
    [InlineData("query ($arg: Boolean = true) { parent @include(if: $arg) { child } test }", @"{ ""arg"": false }", true, true)]

    [InlineData("query ($arg: Boolean!) { parent @skip(if: $arg) { child } test }", @"{ ""arg"": true }", false, true)]
    [InlineData("query ($arg: Boolean!) { parent @skip(if: $arg) { child } test }", @"{ ""arg"": false }", false, false)]
    [InlineData("query ($arg: Boolean!) { parent @skip(if: $arg) { child } test }", @"{ ""arg"": true }", true, true)]
    [InlineData("query ($arg: Boolean!) { parent @skip(if: $arg) { child } test }", @"{ ""arg"": false }", true, true)]
    [InlineData("query ($arg: Boolean!) { parent @include(if: $arg) { child } test }", @"{ ""arg"": true }", false, false)]
    [InlineData("query ($arg: Boolean!) { parent @include(if: $arg) { child } test }", @"{ ""arg"": false }", false, true)]
    [InlineData("query ($arg: Boolean!) { parent @include(if: $arg) { child } test }", @"{ ""arg"": true }", true, true)]
    [InlineData("query ($arg: Boolean!) { parent @include(if: $arg) { child } test }", @"{ ""arg"": false }", true, true)]

    [InlineData("{ ... @skip(if: true) { parent { child } } test }", null, false, true)]
    [InlineData("{ ... @skip(if: false) { parent { child } } test }", null, false, false)]
    [InlineData("{ ... @skip(if: true) { parent { child } } test }", null, true, true)]
    [InlineData("{ ... @skip(if: false) { parent { child } } test }", null, true, true)]

    [InlineData("{ ...frag1 @skip(if: true) test } fragment frag1 on QueryType { parent { child } }", null, false, true)]
    [InlineData("{ ...frag1 @skip(if: false) test } fragment frag1 on QueryType { parent { child } }", null, false, false)]
    [InlineData("{ ...frag1 @skip(if: true) test } fragment frag1 on QueryType { parent { child } }", null, true, true)]
    [InlineData("{ ...frag1 @skip(if: false) test } fragment frag1 on QueryType { parent { child } }", null, true, true)]

    [InlineData("fragment frag1 on QueryType { parent { child } } { ...frag1 @skip(if: true) test }", null, false, true)]
    [InlineData("fragment frag1 on QueryType { parent { child } } { ...frag1 @skip(if: false) test }", null, false, false)]
    [InlineData("fragment frag1 on QueryType { parent { child } } { ...frag1 @skip(if: true) test }", null, true, true)]
    [InlineData("fragment frag1 on QueryType { parent { child } } { ...frag1 @skip(if: false) test }", null, true, true)]

    [InlineData("{ parent @skip(if: true) { ...frag1 } test } fragment frag1 on ChildType { child }", null, false, true)]
    [InlineData("{ parent @skip(if: false) { ...frag1 } test } fragment frag1 on ChildType { child }", null, false, false)]
    [InlineData("{ parent @skip(if: true) { ...frag1 } test } fragment frag1 on ChildType { child }", null, true, true)]
    [InlineData("{ parent @skip(if: false) { ...frag1 } test } fragment frag1 on ChildType { child }", null, true, true)]

    [InlineData("fragment frag1 on ChildType { child } { parent @skip(if: true) { ...frag1 } test }", null, false, true)]
    [InlineData("fragment frag1 on ChildType { child } { parent @skip(if: false) { ...frag1 } test }", null, false, false)]
    [InlineData("fragment frag1 on ChildType { child } { parent @skip(if: true) { ...frag1 } test }", null, true, true)]
    [InlineData("fragment frag1 on ChildType { child } { parent @skip(if: false) { ...frag1 } test }", null, true, true)]
    public void SkipInclude(string query, string? variables, bool authenticated, bool expectedIsValid)
    {
        _field.Authorize();
        _query.AddField(new FieldType { Name = "test", Type = typeof(StringGraphType) });
        if (authenticated)
            SetAuthorized();

        var ret = Validate(query, variables: variables);
        ret.IsValid.ShouldBe(expectedIsValid);
    }

    [Theory]
    [InlineData(Mode.Authorize, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, -1, "Access denied for schema.")]
    [InlineData(Mode.None, Mode.Authorize, Mode.None, Mode.None, Mode.None, Mode.None, 1, "Access denied for type 'QueryType' for query operation.")]
    [InlineData(Mode.None, Mode.None, Mode.Authorize, Mode.None, Mode.None, Mode.None, 3, "Access denied for field 'parent' on type 'QueryType'.")]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.Authorize, Mode.None, Mode.None, 3, "Access denied for type 'ChildType' for field 'parent' on type 'QueryType'.")]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.Authorize, Mode.None, 12, "Access denied for field 'child' on type 'ChildType'.")]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.Authorize, 18, "Access denied for argument 'arg' for field 'child' on type 'ChildType'.")]
    public void ErrorMessages(Mode schemaMode, Mode queryMode, Mode fieldMode, Mode childMode, Mode childFieldMode, Mode argumentMode, int column, string message)
    {
        Apply(_schema, schemaMode);
        Apply(_query, queryMode);
        Apply(_field, fieldMode);
        Apply(_childGraph, childMode);
        Apply(_childField, childFieldMode);
        Apply(_argument, argumentMode);
        var ret = Validate(@"{ parent { child(arg: null) } }");
        ret.IsValid.ShouldBeFalse();
        ret.Errors.Count.ShouldBe(1);
        ret.Errors[0].Message.ShouldBe(message);
        if (column == -1)
            ret.Errors[0].Locations.ShouldBeNull();
        else
        {
            ret.Errors[0].Locations.ShouldNotBeNull();
            ret.Errors[0].Locations!.Count.ShouldBe(1);
            ret.Errors[0].Locations![0].Line.ShouldBe(1);
            ret.Errors[0].Locations![0].Column.ShouldBe(column);
        }
        var err = ret.Errors[0].ShouldBeOfType<AccessDeniedError>();
        err.Code.ShouldBe("ACCESS_DENIED");
        err.PolicyAuthorizationResult.ShouldBeNull();
        err.PolicyRequired.ShouldBeNull();
        err.RolesRequired.ShouldBeNull();
    }

    [Theory]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.Authorize, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.Authorize, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.RoleSuccess, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.RoleSuccess, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.RoleFailure, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.RoleFailure, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.RoleMultiple, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.RoleMultiple, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.PolicySuccess, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.PolicySuccess, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.PolicyFailure, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.PolicyFailure, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.PolicyMultiple, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.PolicyMultiple, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.Anonymous, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, false, true)]
    [InlineData(Mode.Anonymous, Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.Authorize, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.Authorize, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.RoleSuccess, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.RoleSuccess, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.RoleFailure, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.RoleFailure, Mode.None, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.RoleMultiple, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.RoleMultiple, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.PolicySuccess, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.PolicySuccess, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.PolicyFailure, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.PolicyFailure, Mode.None, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.PolicyMultiple, Mode.None, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.PolicyMultiple, Mode.None, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.Anonymous, Mode.None, Mode.None, Mode.None, Mode.None, false, true)]
    [InlineData(Mode.None, Mode.Anonymous, Mode.None, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.Authorize, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.Authorize, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.RoleSuccess, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.RoleSuccess, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.RoleFailure, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.RoleFailure, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.RoleMultiple, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.RoleMultiple, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.PolicySuccess, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.PolicySuccess, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.PolicyFailure, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.PolicyFailure, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.PolicyMultiple, Mode.None, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.PolicyMultiple, Mode.None, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.Anonymous, Mode.None, Mode.None, Mode.None, false, true)]
    [InlineData(Mode.None, Mode.None, Mode.Anonymous, Mode.None, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.Authorize, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.Authorize, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.RoleSuccess, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.RoleSuccess, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.RoleFailure, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.RoleFailure, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.RoleMultiple, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.RoleMultiple, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.PolicySuccess, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.PolicySuccess, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.PolicyFailure, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.PolicyFailure, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.PolicyMultiple, Mode.None, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.PolicyMultiple, Mode.None, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.Anonymous, Mode.None, Mode.None, false, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.Anonymous, Mode.None, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.Authorize, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.Authorize, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleSuccess, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleSuccess, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleFailure, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleFailure, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleMultiple, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleMultiple, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicySuccess, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicySuccess, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicyFailure, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicyFailure, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicyMultiple, Mode.None, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicyMultiple, Mode.None, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.Anonymous, Mode.None, false, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.Anonymous, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.Authorize, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.Authorize, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleSuccess, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleSuccess, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleFailure, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleFailure, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleMultiple, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.RoleMultiple, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicySuccess, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicySuccess, true, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicyFailure, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicyFailure, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicyMultiple, false, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.PolicyMultiple, true, false)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.Anonymous, false, true)]
    [InlineData(Mode.None, Mode.None, Mode.None, Mode.None, Mode.None, Mode.Anonymous, true, true)]
    [InlineData(Mode.Authorize, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, false, false)]
    [InlineData(Mode.Authorize, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, true, true)]
    [InlineData(Mode.Anonymous, Mode.Authorize, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, false, true)] // query is authorize, but field is anonymous
    [InlineData(Mode.Anonymous, Mode.Authorize, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, true, true)]
    [InlineData(Mode.Anonymous, Mode.Anonymous, Mode.Authorize, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, false, false)]
    [InlineData(Mode.Anonymous, Mode.Anonymous, Mode.Authorize, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, true, true)]
    [InlineData(Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Authorize, Mode.Anonymous, Mode.Anonymous, false, true)] // child graph is authorize, but child field is anonymous
    [InlineData(Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Authorize, Mode.Anonymous, Mode.Anonymous, true, true)]
    [InlineData(Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Authorize, Mode.Anonymous, false, false)]
    [InlineData(Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Authorize, Mode.Anonymous, true, true)]
    [InlineData(Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Authorize, false, false)]
    [InlineData(Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Anonymous, Mode.Authorize, true, true)]
    public void Matrix(Mode schemaMode, Mode queryMode, Mode fieldMode, Mode childMode, Mode childFieldMode, Mode argumentMode, bool authenticated, bool isValid)
    {
        Apply(_schema, schemaMode);
        Apply(_query, queryMode);
        Apply(_field, fieldMode);
        Apply(_childGraph, childMode);
        Apply(_childField, childFieldMode);
        Apply(_argument, argumentMode);
        if (authenticated)
            SetAuthorized();

        // simple test
        var ret = Validate(@"{ parent { child(arg: null) } }");
        ret.IsValid.ShouldBe(isValid);

        // non-null test
        _field.ResolvedType = new NonNullGraphType(_childGraph);
        ret = Validate(@"{ parent { child(arg: null) } }");
        ret.IsValid.ShouldBe(isValid);

        // list test
        _field.ResolvedType = new ListGraphType(_childGraph);
        ret = Validate(@"{ parent { child(arg: null) } }");
        ret.IsValid.ShouldBe(isValid);

        // non-null list of non-null test
        _field.ResolvedType = new NonNullGraphType(new ListGraphType(new NonNullGraphType(_childGraph)));
        ret = Validate(@"{ parent { child(arg: null) } }");
        ret.IsValid.ShouldBe(isValid);

        //reset
        _field.ResolvedType = _childGraph;

        // inline fragment
        ret = Validate(@"{ parent { ... on ChildType { child(arg: null) } } }");
        ret.IsValid.ShouldBe(isValid);

        // fragment prior to query
        ret = Validate(@"fragment frag on ChildType { child(arg: null) } { parent { ...frag } }");
        ret.IsValid.ShouldBe(isValid);

        // fragment after query
        ret = Validate(@"{ parent { ...frag } } fragment frag on ChildType { child(arg: null) }");
        ret.IsValid.ShouldBe(isValid);

        // nested fragments prior to query
        ret = Validate(@"fragment nestedFrag on ChildType { child(arg: null) } fragment frag on ChildType { ...nestedFrag } { parent { ...frag } }");
        ret.IsValid.ShouldBe(isValid);

        // nested fragments after query
        ret = Validate(@"{ parent { ...frag } } fragment frag on ChildType { ...nestedFrag } fragment nestedFrag on ChildType { child(arg: null) }");
        ret.IsValid.ShouldBe(isValid);

        // nested fragments around query 1
        ret = Validate(@"fragment frag on ChildType { ...nestedFrag } { parent { ...frag } } fragment nestedFrag on ChildType { child(arg: null) }");
        ret.IsValid.ShouldBe(isValid);

        // nested fragments around query 2
        ret = Validate(@"fragment nestedFrag on ChildType { child(arg: null) } { parent { ...frag } } fragment frag on ChildType { ...nestedFrag }");
        ret.IsValid.ShouldBe(isValid);
    }

    [Theory]
    [InlineData(Mode.None, Mode.None, false, true)]
    [InlineData(Mode.None, Mode.None, true, true)]
    [InlineData(Mode.Authorize, Mode.None, false, false)] // schema requires authentication, so introspection queries fail
    [InlineData(Mode.Authorize, Mode.None, true, true)]
    [InlineData(Mode.None, Mode.Authorize, false, true)]  // query type requires authentication, but __schema is an AllowAnonymous type
    [InlineData(Mode.None, Mode.Authorize, true, true)]
    public void Introspection(Mode schemaMode, Mode queryMode, bool authenticated, bool isValid)
    {
        Apply(_schema, schemaMode);
        Apply(_query, queryMode);
        if (authenticated)
            SetAuthorized();

        var ret = Validate(@"{ __schema { types { name } } __typename }");
        ret.IsValid.ShouldBe(isValid);

        ret = Validate(@"{ __schema { types { name } } }");
        ret.IsValid.ShouldBe(isValid);

        ret = Validate(@"{ __type(name: ""QueryType"") { name } }");
        ret.IsValid.ShouldBe(isValid);

        ret = Validate(@"{ __type(name: ""QueryType"") { name } __typename }");
        ret.IsValid.ShouldBe(isValid);
    }

    [Theory]
    [InlineData(Mode.None, Mode.None, false, false, true)]
    [InlineData(Mode.None, Mode.None, false, true, true)]
    [InlineData(Mode.None, Mode.None, true, false, true)]
    [InlineData(Mode.None, Mode.None, true, true, true)]
    [InlineData(Mode.Authorize, Mode.None, false, false, false)] // selecting only __typename is not enough to allow QueryType to pass validation
    [InlineData(Mode.Authorize, Mode.None, false, true, true)]
    [InlineData(Mode.Authorize, Mode.None, true, false, false)]
    [InlineData(Mode.Authorize, Mode.None, true, true, true)]
    [InlineData(Mode.Authorize, Mode.Anonymous, false, false, false)]
    [InlineData(Mode.Authorize, Mode.Anonymous, false, true, true)]
    [InlineData(Mode.Authorize, Mode.Anonymous, true, false, true)] // at least only anonymous field, and no authenticated fields, were selected in the query, so validation passes
    [InlineData(Mode.Authorize, Mode.Anonymous, true, true, true)]
    public void OnlyTypeName(Mode queryMode, Mode fieldMode, bool includeField, bool authenticated, bool isValid)
    {
        Apply(_query, queryMode);
        Apply(_field, fieldMode);
        if (authenticated)
            SetAuthorized();

        IValidationResult ret;

        if (includeField)
        {
            ret = Validate("{ __typename parent { child } }");
        }
        else
        {
            ret = Validate("{ __typename }");
        }
        ret.IsValid.ShouldBe(isValid);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BothAuthorizedAndAnonymousFields(bool authenticated)
    {
        Apply(_query, Mode.Authorize);
        _query.AddField(new FieldType { Name = "test", Type = typeof(StringGraphType) }).AllowAnonymous();
        if (authenticated)
            SetAuthorized();

        var ret = Validate("{ parent { child } test }");
        ret.IsValid.ShouldBe(authenticated);
    }

    [Fact]
    public void UnusedOperationsAreIgnored()
    {
        Apply(_field, Mode.Authorize);
        Apply(_childGraph, Mode.Authorize);
        _query.AddField(new FieldType { Name = "test", Type = typeof(StringGraphType) });
        var ret = Validate("query op1 { test } query op2 { parent { child } }");
        ret.IsValid.ShouldBeTrue();
        ret = Validate("query op1 { parent { child } } query op2 { test }");
        ret.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void UnusedFragmentsAreIgnored()
    {
        Apply(_field, Mode.Authorize);
        Apply(_childGraph, Mode.Authorize);
        _query.AddField(new FieldType { Name = "test", Type = typeof(StringGraphType) });
        var ret = Validate("query op1 { ...frag1 } query op2 { ...frag2 } fragment frag1 on QueryType { test } fragment frag2 on QueryType { parent { child } }");
        ret.IsValid.ShouldBeTrue();
        ret = Validate("query op1 { ...frag1 } query op2 { ...frag2 } fragment frag1 on QueryType { parent { child } } fragment frag2 on QueryType { test }");
        ret.IsValid.ShouldBeFalse();
    }

    private void Apply(IProvideMetadata obj, Mode mode)
    {
        switch (mode)
        {
            case Mode.None:
                break;
            case Mode.RoleSuccess:
                obj.AuthorizeWithRoles("MyRole");
                break;
            case Mode.RoleFailure:
                obj.AuthorizeWithRoles("FailingRole");
                break;
            case Mode.RoleMultiple:
                obj.AuthorizeWithRoles("MyRole", "FailingRole");
                break;
            case Mode.Authorize:
                obj.Authorize();
                break;
            case Mode.PolicySuccess:
                obj.AuthorizeWithPolicy("MyPolicy");
                break;
            case Mode.PolicyFailure:
                obj.AuthorizeWithPolicy("FailingPolicy");
                break;
            case Mode.PolicyMultiple:
                obj.AuthorizeWithPolicy("MyPolicy");
                obj.AuthorizeWithPolicy("FailingPolicy");
                break;
            case Mode.Anonymous:
                obj.AllowAnonymous();
                break;
        }
    }

    [Fact]
    public void Constructors()
    {
        Should.Throw<ArgumentNullException>(() => new AuthorizationVisitor(null!, _principal, Mock.Of<IAuthorizationService>()));
        Should.Throw<ArgumentNullException>(() => new AuthorizationVisitor(new ValidationContext(), null!, Mock.Of<IAuthorizationService>()));
        Should.Throw<ArgumentNullException>(() => new AuthorizationVisitor(new ValidationContext(), _principal, null!));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void MiscErrors(bool noClaimsPrincipal, bool noRequestServices, bool noAuthenticationService)
    {
        var mockAuthorizationService = new Mock<IAuthorizationService>(MockBehavior.Strict);
        mockAuthorizationService.Setup(x => x.AuthorizeAsync(_principal, null, It.IsAny<string>())).Returns<ClaimsPrincipal, object, string>((_, _, policy) =>
        {
            if (policy == "MyPolicy" && _policyPasses)
                return Task.FromResult(AuthorizationResult.Success());
            return Task.FromResult(AuthorizationResult.Failed());
        });
        var mockServices = new Mock<IServiceProvider>(MockBehavior.Strict);
        mockServices.Setup(x => x.GetService(typeof(IAuthorizationService))).Returns(noAuthenticationService ? null! : mockAuthorizationService.Object);
        var document = GraphQLParser.Parser.Parse("{ __typename }");
        var validator = new DocumentValidator();

        var err = Should.Throw<Exception>(() => validator.ValidateAsync(new ValidationOptions
        {
            Document = document,
            Extensions = Inputs.Empty,
            Operation = (GraphQLOperationDefinition)document.Definitions.Single(x => x.Kind == ASTNodeKind.OperationDefinition),
            Rules = new IValidationRule[] { new AuthorizationValidationRule() },
            Schema = _schema,
            UserContext = new Dictionary<string, object?>(),
            Variables = Inputs.Empty,
            RequestServices = noRequestServices ? null : mockServices.Object,
            User = noClaimsPrincipal ? null : _principal,
        }).GetAwaiter().GetResult()); // there is no async code being tested

        if (noClaimsPrincipal)
            err.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("User could not be retrieved from ValidationContext. Please be sure it is set in ExecutionOptions.User.");

        if (noRequestServices)
            err.ShouldBeOfType<MissingRequestServicesException>();

        if (noAuthenticationService)
            err.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("An instance of IAuthorizationService could not be pulled from the dependency injection framework.");
    }

    [Fact]
    public void NullIdentity()
    {
        var mockAuthorizationService = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var mockServices = new Mock<IServiceProvider>(MockBehavior.Strict);
        mockServices.Setup(x => x.GetService(typeof(IAuthorizationService))).Returns(mockAuthorizationService.Object);
        var document = GraphQLParser.Parser.Parse("{ __typename }");
        var validator = new DocumentValidator();
        _schema.Authorize();

        var (result, _) = validator.ValidateAsync(new ValidationOptions
        {
            Document = document,
            Extensions = Inputs.Empty,
            Operation = (GraphQLOperationDefinition)document.Definitions.Single(x => x.Kind == ASTNodeKind.OperationDefinition),
            Rules = new IValidationRule[] { new AuthorizationValidationRule() },
            Schema = _schema,
            UserContext = new Dictionary<string, object?>(),
            Variables = Inputs.Empty,
            RequestServices = mockServices.Object,
            User = new ClaimsPrincipal(),
        }).GetAwaiter().GetResult(); // there is no async code being tested

        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<AccessDeniedError>().Message.ShouldBe("Access denied for schema.");
    }

    [Theory]
    [InlineData(false, false, false, false, true)]
    [InlineData(false, false, false, true, true)]
    [InlineData(true, false, false, false, true)]
    [InlineData(true, false, false, true, true)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, true, false, true, true)]

    [InlineData(false, false, true, false, true)]
    [InlineData(false, false, true, true, true)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, false, true, true, true)]
    [InlineData(false, true, true, false, true)]
    [InlineData(false, true, true, true, true)]
    public void WithInterface(bool interfaceRequiresAuth, bool queryRequiresAuth, bool testInterface, bool authenticated, bool expectedIsValid)
    {
        var interfaceGraphType = new InterfaceGraphType { Name = "TestInterface" };
        interfaceGraphType.ResolveType = _ => _query;
        var interfaceField = interfaceGraphType.AddField(new FieldType { Name = "test", Type = typeof(StringGraphType) });
        if (interfaceRequiresAuth)
            interfaceField.Authorize();

        var queryField = _query.AddField(new FieldType { Name = "test", Type = typeof(StringGraphType) });
        _query.AddResolvedInterface(interfaceGraphType);
        if (queryRequiresAuth)
            queryField.Authorize();

        if (authenticated)
            SetAuthorized();

        _schema.RegisterType(interfaceGraphType);

        var ret = Validate(testInterface ? "{ ... on TestInterface { test } }" : "{ test }");
        ret.IsValid.ShouldBe(expectedIsValid);
    }

    // these tests will fail core validation rules and so it does not really matter
    // if they pass or fail authorization, but we are testing them to be sure that
    // they do not cause a fatal exception
    [Theory]
    [InlineData(@"{ test @skip }", null, false)]
    [InlineData(@"{ test @include }", null, false)]
    [InlineData(@"{ test @skip(if: HELLO) }", null, false)]
    [InlineData(@"{ test @include(if: HELLO) }", null, false)]
    [InlineData(@"{ test @skip(if: $arg) }", null, false)]
    [InlineData(@"{ test @include(if: $arg) }", null, false)]
    [InlineData(@"query ($arg2: String) { test @skip(if: $arg) }", null, false)]
    [InlineData(@"query ($arg2: String) { test @include(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: String) { test @skip(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: String) { test @include(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: Boolean = TEST) { test @skip(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: Boolean = TEST) { test @include(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: Boolean) { test @skip(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: Boolean) { test @include(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: Boolean!) { test @skip(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: Boolean!) { test @include(if: $arg) }", null, false)]
    [InlineData(@"query ($arg: Boolean!) { test @skip(if: $arg) }", @"{ ""arg"":""abc"" }", false)]
    [InlineData(@"query ($arg: Boolean!) { test @include(if: $arg) }", @"{ ""arg"":""abc"" }", false)]
    [InlineData(@"{ invalid }", null, true)]
    [InlineData(@"{ invalid { child } }", null, true)]
    [InlineData(@"{ test { child } }", null, false)]
    [InlineData(@"{ parent { invalid } }", null, true)]
    [InlineData(@"{ parent { child(invalid: true) } }", null, true)]
    [InlineData(@"query { ...frag1 }", null, true)]
    [InlineData(@"query { ...frag1 } fragment frag1 on QueryType { ...frag1 }", null, true)]
    public void TestDefective(string query, string variables, bool expectedIsValid)
    {
        _query.AddField(new FieldType { Name = "test", Type = typeof(StringGraphType) }).Authorize();

        var ret = Validate(query, false, variables);
        ret.IsValid.ShouldBe(expectedIsValid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestPipeline(bool authenticated)
    {
        _field.Authorize();

        if (authenticated)
            SetAuthorized();

        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema(_schema)
            .AddSystemTextJson()
            .AddAuthorizationRule());

        services.AddSingleton(Mock.Of<IAuthorizationService>(MockBehavior.Strict));

        using var provider = services.BuildServiceProvider();

        var executer = provider.GetRequiredService<IDocumentExecuter<ISchema>>();
        var ret = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = @"{ parent { child } }",
            RequestServices = provider,
            User = _principal,
        });

        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        var actual = serializer.Serialize(ret);

        if (authenticated)
            actual.ShouldBe(@"{""data"":{""parent"":null}}");
        else
            actual.ShouldBe(@"{""errors"":[{""message"":""Access denied for field \u0027parent\u0027 on type \u0027QueryType\u0027."",""locations"":[{""line"":1,""column"":3}],""extensions"":{""code"":""ACCESS_DENIED"",""codes"":[""ACCESS_DENIED""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EndToEnd(bool authenticated)
    {
        _field.Authorize();

        if (authenticated)
            SetAuthorized();

        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddSchema(_schema)
                .AddSystemTextJson()
                .AddAuthorizationRule());
            services.AddAuthentication();
            services.AddAuthorization();
#if NETCOREAPP2_1 || NET48
            services.AddHostApplicationLifetime();
#endif
        });
        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            // simulate app.UseAuthentication()
            app.Use(next => context =>
            {
                context.User = _principal;
                return next(context);
            });
            app.UseGraphQL();
        });
        using var server = new TestServer(hostBuilder);

        using var client = server.CreateClient();
        using var response = await client.GetAsync("/graphql?query={ parent { child } }");
        response.StatusCode.ShouldBe(authenticated ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.BadRequest);
        var actual = await response.Content.ReadAsStringAsync();

        if (authenticated)
            actual.ShouldBe(@"{""data"":{""parent"":null}}");
        else
            actual.ShouldBe(@"{""errors"":[{""message"":""Access denied for field \u0027parent\u0027 on type \u0027QueryType\u0027."",""locations"":[{""line"":1,""column"":3}],""extensions"":{""code"":""ACCESS_DENIED"",""codes"":[""ACCESS_DENIED""]}}]}");
    }

    public enum Mode
    {
        None,
        Authorize,
        RoleSuccess,
        RoleFailure,
        RoleMultiple,
        PolicySuccess,
        PolicyFailure,
        PolicyMultiple,
        Anonymous,
    }
}

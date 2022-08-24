using System.ComponentModel;
using GraphQL;

namespace AuthorizationSample.Schema;

public class Query
{
    [Description("Does not require authentication.")]
    public static string Hello => "Hello anybody.";

    [Authorize]
    [Description("Requires authentication, but no role membership or specific policy is enforced.")]
    public static string HelloRegisteredUser => "Hello, Registered User!";

    [Authorize(Roles = "User")]
    [Description("Requires membership to the 'User' role.")]
    public static string HelloUser => "Hello, User!";

    [Description("This field does not require authorization, but the argument 'name' does.")]
    public static string HelloPerson([Authorize] string? name) => name ?? "Unknown";

    [Description("This field does not require authorization, but the type it return does")]
    public static Person GetPerson => new Person { Name = "User" };

    [Authorize("MyPolicy")]
    [Description("This field requires the 'MyPolicy' policy (which requires the User role) to pass authorization.")]
    public static string HelloByPolicy => "Policy Passed!";
}

[Authorize]
public class Person
{
    public string Name { get; set; } = null!;
}

[Authorize("MyPolicy")] // this policy requires the User role
public class Mutation
{
    [Description("No requirement is defined on this field, but the Mutation type requires the 'MyPolicy' policy (which requires the User role) to pass authorization.")]
    public static string Hello => "Hello authenticated user.";

    [AllowAnonymous]
    [Description("Although the mutation type requires authentication, this field does not, so as long as only this field is selected, authorization will pass.")]
    public static string Unprotected => "Hello anybody.";
}

# Migrating from v7 to v8

## New features

- When using `FormFileGraphType` with type-first schemas, you may specify the allowed media
  types for the file by using the new `[MediaType]` attribute on the argument or input object field.
- Cross-site request forgery (CSRF) protection has been added for both GET and POST requests,
  enabled by default.

## Breaking changes

- The validation rules' signatures have changed slightly due to the underlying changes to the
  GraphQL.NET library.  Please see the GraphQL.NET v8 migration document for more information.
- The obsolete (v6 and prior) authorization validation rule has been removed.  See the v7 migration
  document for more information on how to migrate to the v7/v8 authorization validation rule.
- Cross-site request forgery (CSRF) protection has been enabled for all requests by default.
  This will require that the `GraphQL-Require-Preflight` header be sent with all GET requests and
  all form-POST requests.  To disable this feature, set the `CsrfProtectionEnabled` property on the
  `GraphQLMiddlewareOptions` class to `false`.  You may also configure the headers list by modifying
  the `CsrfProtectionHeaders` property on the same class.  See the readme for more details.

## Other changes

- GraphiQL has been bumped from 1.5.1 to 3.2.0.

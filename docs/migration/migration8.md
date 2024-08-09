# Migrating from v7 to v8

## New features

- When using `FormFileGraphType` with type-first schemas, you may specify the allowed media
  types for the file by using the new `[MediaType]` attribute on the argument or input object field.
- Cross-site request forgery (CSRF) protection has been added for both GET and POST requests,
  enabled by default.
- Status codes for validation errors are now, by default, determined by the response content type,
  and for authentication errors may return a 401 or 403 status code.  These changes are purusant
  to the [GraphQL over HTTP specification](https://github.com/graphql/graphql-over-http/blob/main/spec/GraphQLOverHTTP.md).
  See the breaking changes section below for more information.

## Breaking changes

- `GraphQLHttpMiddlewareOptions.ValidationErrorsReturnBadRequest` is now a nullable boolean where
   `null` means "use the default behavior".  The default behavior is to return a 200 status code
  when the response content type is `application/json` and a 400 status code otherwise.  The
  default value for this in v7 was `true`; set this option to retain the v7 behavior.
- The validation rules' signatures have changed slightly due to the underlying changes to the
  GraphQL.NET library.  Please see the GraphQL.NET v8 migration document for more information.
- The obsolete (v6 and prior) authorization validation rule has been removed.  See the v7 migration
  document for more information on how to migrate to the v7/v8 authorization validation rule.
- Cross-site request forgery (CSRF) protection has been enabled for all requests by default.
  This will require that the `GraphQL-Require-Preflight` header be sent with all GET requests and
  all form-POST requests.  To disable this feature, set the `CsrfProtectionEnabled` property on the
  `GraphQLMiddlewareOptions` class to `false`.  You may also configure the headers list by modifying
  the `CsrfProtectionHeaders` property on the same class.  See the readme for more details.
- Form POST requests are disabled by default; to enable them, set the `ReadFormOnPost` setting
  to `true`.
- Validation errors such as authentication errors may now be returned with a 'preferred' status
  code instead of a 400 status code.  This occurs when (1) the response would otherwise contain
  a 400 status code (e.g. the execution of the document has not yet begun), and (2) all errors
  in the response prefer the same status code.  For practical purposes, this means that the included
  errors triggered by the authorization validation rule will now return 401 or 403 when appropriate.
- The `SelectResponseContentType` method now returns a `MediaTypeHeaderValue` instead of a string.

## Other changes

- GraphiQL has been bumped from 1.5.1 to 3.2.0.

# Migrating from v7 to v8

## New features

- When using `FormFileGraphType` with type-first schemas, you may specify the allowed media
  types for the file by using the new `[MediaType]` attribute on the argument or input object field.

## Breaking changes

- The validation rules' signatures have changed slightly due to the underlying changes to the
  GraphQL.NET library.  Please see the GraphQL.NET v8 migration document for more information.
- The obsolete (v6 and prior) authorization validation rule has been removed.  See the v7 migration
  document for more information on how to migrate to the v7/v8 authorization validation rule.

## Other changes

- GraphiQL has been bumped from 1.5.1 to 3.2.0.

GraphQL for .NET - Subscription Transport WebSockets
====================================================
>WARNING: not ready for production use!

[![Build Status](https://ci.appveyor.com/api/projects/status/github/graphql-dotnet/subscription-transport-ws?branch=master&svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/subscriptions-transport-ws)

## Sample

Samples.Server shows a simple Chat style example of how subscription transport is used.

Here are example queries to get started. Use three browser tabs or better yet windows 
to view the changes.

## sub 1

Query:

```
subscription MessageAddedByUser($id:String!) {
  messageAddedByUser(id: $id) {
    from { id displayName }
    content
  }
}
```

Variables:

```
{
  "id": "1"
}
```

## sub 2

```
subscription MessageAdded {
  messageAdded {
    from { id displayName }
    content
  }
}
```

## Mutation

Query:

```
mutation AddMessage($message: MessageInputType!) {
  addMessage(message: $message) {
    from {
      id
      displayName
    }
    content
  }
}
```

Variables: 

```
{
  "message": {
    "content": "Message",
    "fromId": "1"
  }
}
```
GraphQL for .NET - Subscription Transport WebSockets
====================================================

>WARNING: not ready for production use!

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
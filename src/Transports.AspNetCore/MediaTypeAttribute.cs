using System.Collections;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Ensures that the marked field argument or input object field is a valid media type
/// via the <see cref="MediaTypeHeaderValue.IsSubsetOf(MediaTypeHeaderValue)"/> method
/// and supports wildcards such as "<c>text/*</c>".
/// </summary>
/// <remarks>
/// Only checks values of type <see cref="IFormFile"/>, or lists of <see cref="IFormFile"/>.
/// Any other types of values will throw a run-time exception.
/// </remarks>
public class MediaTypeAttribute : GraphQLAttribute
{
    private readonly MediaTypeHeaderValue[] _mimeTypes;

    /// <inheritdoc cref="MediaTypeAttribute"/>
    public MediaTypeAttribute(params string[] mimeTypes)
    {
        var types = MediaTypeHeaderValue.ParseList(mimeTypes);
        _mimeTypes = types as MediaTypeHeaderValue[] ?? types.ToArray();
    }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        var lists = fieldType.Type != null
            ? CountNestedLists(fieldType.Type)
            : CountNestedLists(fieldType.ResolvedType
                ?? throw new InvalidOperationException($"No graph type set on field '{fieldType.Name}'."));
        fieldType.Validator += new Validator(lists, _mimeTypes).Validate;
    }

    /// <inheritdoc/>
    public override void Modify(QueryArgument queryArgument)
    {
        var lists = queryArgument.Type != null
            ? CountNestedLists(queryArgument.Type)
            : CountNestedLists(queryArgument.ResolvedType
                ?? throw new InvalidOperationException($"No graph type set on field '{queryArgument.Name}'."));
        queryArgument.Validate(new Validator(lists, _mimeTypes).Validate);
    }

    private class Validator
    {
        private readonly int _lists;
        private readonly MediaTypeHeaderValue[] _mediaTypes;

        public Validator(int lists, MediaTypeHeaderValue[] mediaTypes)
        {
            _lists = lists;
            _mediaTypes = mediaTypes;
        }

        public void Validate(object? obj)
        {
            Validate(obj, _lists);
        }

        public void Validate(object? obj, int lists)
        {
            if (obj == null)
                return;
            if (lists == 0)
            {
                if (obj is IFormFile file)
                    ValidateMediaType(file);
                else
                    throw new InvalidOperationException("Expected an IFormFile object.");
            }
            else if (obj is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    Validate(item, lists - 1);
                }
            }
            else
            {
                throw new InvalidOperationException("Expected a list.");
            }
        }

        public void ValidateMediaType(IFormFile? file)
        {
            if (file == null)
                return;
            var contentType = file.ContentType;
            if (contentType == null)
                return;
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            foreach (var validMediaType in _mediaTypes)
            {
                if (mediaType.IsSubsetOf(validMediaType))
                    return;
            }
            throw new InvalidOperationException($"Invalid media type '{mediaType}'.");
        }
    }
    private static int CountNestedLists(Type type)
    {
        if (!type.IsGenericType)
            return 0;

        var typeDef = type.GetGenericTypeDefinition();

        if (typeDef == typeof(ListGraphType<>))
        {
            return 1 + CountNestedLists(type.GetGenericArguments()[0]);
        }

        if (typeDef == typeof(NonNullGraphType<>))
        {
            return CountNestedLists(type.GetGenericArguments()[0]);
        }

        return 0;
    }

    private static int CountNestedLists(IGraphType type)
    {
        if (type is ListGraphType listGraphType)
        {
            return 1 + CountNestedLists(listGraphType.ResolvedType ?? throw new InvalidOperationException($"ResolvedType not set for {listGraphType}."));
        }

        if (type is NonNullGraphType nonNullGraphType)
        {
            return CountNestedLists(nonNullGraphType.ResolvedType ?? throw new InvalidOperationException($"ResolvedType not set for {nonNullGraphType}."));
        }

        return 0;
    }
}

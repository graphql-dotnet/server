using GraphQLParser;
using GraphQLParser.Visitors;

namespace GraphQL.Server.Transports.AspNetCore;

public partial class AuthorizationVisitorBase
{
    /// <summary>
    /// Returns all fragments referenced by the selected operation in the document,
    /// excluding ones that would be skipped by the @skip or @include directives.
    /// </summary>
    /// <remarks>
    /// <see cref="SkipNode(ASTNode, ValidationContext)"/> is used to determine if the node should be skipped or not.
    /// </remarks>
    protected List<GraphQLFragmentDefinition>? GetRecursivelyReferencedUsedFragments(ValidationContext validationContext)
    {
        var context = new GetRecursivelyReferencedFragmentsVisitorContext(this, validationContext);
        var ret = GetRecursivelyReferencedFragmentsVisitor.Instance.VisitAsync(validationContext.Operation, context);
        if (!ret.IsCompletedSuccessfully) // should always be true unless an exception occurs within SkipNode
            ret.AsTask().GetAwaiter().GetResult();
        return context.FragmentDefinitions;
    }

    private sealed class GetRecursivelyReferencedFragmentsVisitorContext : IASTVisitorContext
    {
        public GetRecursivelyReferencedFragmentsVisitorContext(AuthorizationVisitorBase authorizationVisitor, ValidationContext validationContext)
        {
            AuthorizationVisitor = authorizationVisitor;
            ValidationContext = validationContext;
        }

        public AuthorizationVisitorBase AuthorizationVisitor { get; }

        public CancellationToken CancellationToken => default;

        public ValidationContext ValidationContext { get; }

        public List<GraphQLFragmentDefinition>? FragmentDefinitions { get; set; }
    }

    private sealed class GetRecursivelyReferencedFragmentsVisitor : ASTVisitor<GetRecursivelyReferencedFragmentsVisitorContext>
    {
        private GetRecursivelyReferencedFragmentsVisitor() { }

        public static readonly GetRecursivelyReferencedFragmentsVisitor Instance = new();

        public override ValueTask VisitAsync(ASTNode? node, GetRecursivelyReferencedFragmentsVisitorContext context)
        {
            // check if this node should be skipped or not (check @skip and @include directives)
            if (node == null || !context.AuthorizationVisitor.SkipNode(node, context.ValidationContext))
                return base.VisitAsync(node, context);

            return default;
        }

        protected override ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, GetRecursivelyReferencedFragmentsVisitorContext context)
            => VisitAsync(operationDefinition.SelectionSet, context);

        protected override ValueTask VisitSelectionSetAsync(GraphQLSelectionSet selectionSet, GetRecursivelyReferencedFragmentsVisitorContext context)
            => VisitAsync(selectionSet.Selections, context);

        protected override ValueTask VisitFieldAsync(GraphQLField field, GetRecursivelyReferencedFragmentsVisitorContext context)
            => VisitAsync(field.SelectionSet, context);

        protected override ValueTask VisitInlineFragmentAsync(GraphQLInlineFragment inlineFragment, GetRecursivelyReferencedFragmentsVisitorContext context)
            => VisitAsync(inlineFragment.SelectionSet, context);

        protected override ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, GetRecursivelyReferencedFragmentsVisitorContext context)
        {
            // if we have not encountered this fragment before
            if (!Contains(fragmentSpread, context))
            {
                // find the fragment definition
                var fragmentDefinition = context.ValidationContext.Document.FindFragmentDefinition(fragmentSpread.FragmentName.Name);
                if (fragmentDefinition != null)
                {
                    // add the fragment definition to our known list
                    (context.FragmentDefinitions ??= new()).Add(fragmentDefinition);
                    // walk the fragment definition
                    return VisitSelectionSetAsync(fragmentDefinition.SelectionSet, context);
                }
            }

            return default;
        }

        private static bool Contains(GraphQLFragmentSpread fragmentSpread, GetRecursivelyReferencedFragmentsVisitorContext context)
        {
            var fragmentDefinitions = context.FragmentDefinitions;
            if (fragmentDefinitions == null)
                return false;

            foreach (var fragmentDefinition in fragmentDefinitions)
            {
                if (fragmentDefinition.FragmentName.Name == fragmentSpread.FragmentName.Name)
                    return true;
            }

            return false;
        }
    }
}

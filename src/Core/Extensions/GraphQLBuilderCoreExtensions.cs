#nullable enable

using System;
using GraphQL.DI;
using GraphQL.Instrumentation;
using GraphQL.Types;

namespace GraphQL.Server
{
    /// <summary>
    /// GraphQL specific extension methods for <see cref="IGraphQLBuilder"/>.
    /// </summary>
    public static class GraphQLBuilderCoreExtensions
    {
        /// <summary>
        /// Registers an instance of <see cref="BasicGraphQLExecuter{TSchema}"/> as <see cref="IGraphQLExecuter{TSchema}"/> for
        /// use with <see cref="GraphQLOptions"/>. It is recommended to use <see cref="IDocumentExecuter"/> directly rather than
        /// use the <see cref="IGraphQLExecuter{TSchema}"/> interface.
        /// <br/><br/>
        /// Also installs <see cref="InstrumentFieldsMiddleware"/> in the schema if specified. (Previous' versions of
        /// GraphQL.Server would install this middleware automatically.) Specify <see langword="false"/> for <paramref name="installMetricsMiddleware"/>
        /// if <see cref="GraphQLBuilderExtensions.AddMetrics(IGraphQLBuilder, bool)"/> will be called separately. Note that
        /// the implementation of <see cref="BasicGraphQLExecuter{TSchema}"/> will set <see cref="ExecutionOptions.EnableMetrics"/>
        /// if <see cref="GraphQLOptions.EnableMetrics"/> is set.
        /// </summary>
        public static IGraphQLBuilder AddServer(this IGraphQLBuilder builder, bool installMetricsMiddleware, Action<GraphQLOptions>? configureOptions)
        {
            builder.Services.TryRegister(typeof(IGraphQLExecuter<>), typeof(BasicGraphQLExecuter<>), ServiceLifetime.Transient);
            builder.Services.TryRegister(typeof(IGraphQLExecuter), typeof(BasicGraphQLExecuter<ISchema>), ServiceLifetime.Transient);
            builder.Services.Configure(configureOptions);
            if (installMetricsMiddleware)
                builder.AddMetrics(false);
            return builder;
        }

        /// <inheritdoc cref="AddServer(IGraphQLBuilder, bool, Action{GraphQLOptions})"/>
        public static IGraphQLBuilder AddServer(this IGraphQLBuilder builder, bool installMetricsMiddleware, Action<GraphQLOptions, IServiceProvider>? configureOptions = null)
        {
            builder.Services.TryRegister(typeof(IGraphQLExecuter<>), typeof(BasicGraphQLExecuter<>), ServiceLifetime.Transient);
            builder.Services.TryRegister(typeof(IGraphQLExecuter), typeof(BasicGraphQLExecuter<ISchema>), ServiceLifetime.Transient);
            builder.Services.Configure(configureOptions);
            if (installMetricsMiddleware)
                builder.AddMetrics(false);
            return builder;
        }
    }
}

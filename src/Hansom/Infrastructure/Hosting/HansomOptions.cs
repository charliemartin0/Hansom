using System.Reflection;
using Hansom.Application.Execution;
using Hansom.Application.Routing;

namespace Hansom;

public sealed class HansomOptions
{
    public ICollection<Assembly> HandlerAssemblies { get; } = new List<Assembly>();

    /// <summary>
    /// The routing catalog the host registers. Fluent routing registrations made in the
    /// configure callback land here, so the resolved RoutingCatalog is the configured one.
    /// </summary>
    internal RoutingCatalog Routing { get; } = new();

    /// <summary>
    /// The error-policy catalog the host registers. Fluent policy registrations made in the
    /// configure callback land here, so the resolved ErrorPolicyCatalog is the configured one.
    /// </summary>
    internal ErrorPolicyCatalog Policies { get; } = new();

    public PublishExpression PublishMessage<T>() => Routing.PublishMessage<T>();

    public OnExceptionExpression OnException<TException>()
        where TException : Exception =>
        Policies.OnException<TException>();
}
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace MiniVerine.Application.Cascades;

/// <summary>
/// Cached, compiled access to <c>Task&lt;T&gt;.Result</c>. Keyed by the closed runtime
/// task type (<c>task.GetType()</c>), so each distinct <c>Task&lt;T&gt;</c> instantiation
/// compiles one unwrap delegate instead of reflecting <c>GetProperty</c> per call.
/// </summary>
internal static class TaskResultAccessor
{
    internal static ConcurrentDictionary<Type, Func<Task, object?>> Cache { get; } = new();

    public static object? GetResult(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        Type taskType = task.GetType();
        if (!taskType.IsGenericType)
        {
            return null;
        }

        return Cache.GetOrAdd(taskType, Compile)(task);
    }

    private static Func<Task, object?> Compile(Type taskType)
    {
        ParameterExpression taskParameter = Expression.Parameter(typeof(Task), "task");
        Expression instance = Expression.Convert(taskParameter, taskType);
        PropertyInfo resultProperty = taskType.GetProperty(nameof(Task<object>.Result))
            ?? throw new InvalidOperationException($"'{taskType}' has no Result property.");
        Expression body = Expression.Convert(Expression.Property(instance, resultProperty), typeof(object));
        return Expression.Lambda<Func<Task, object?>>(body, taskParameter).Compile();
    }
}
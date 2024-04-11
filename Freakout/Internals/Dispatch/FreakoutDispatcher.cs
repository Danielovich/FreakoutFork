﻿using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout.Internals.Dispatch;

class FreakoutDispatcher(ICommandSerializer commandSerializer, IServiceScopeFactory serviceScopeFactory)
{
    readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _invokers = new();

    public async Task ExecuteAsync(OutboxCommand outboxCommand, CancellationToken cancellationToken)
    {
        var command = commandSerializer.Deserialize(outboxCommand);
        var type = command.GetType();

        var invoker = _invokers.GetOrAdd(type, CreateInvoker);

        await invoker(command, cancellationToken);
    }

    async Task ExecuteOutboxCommandGeneric<TCommand>(TCommand command, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand>>();

        await handler.HandleAsync(command, cancellationToken);
    }

    /// <summary>
    /// This is how you build an invoker for a generic method using expression trees
    /// </summary>
    Func<object, CancellationToken, Task> CreateInvoker(Type commandType)
    {
        const string methodName = nameof(ExecuteOutboxCommandGeneric);

        // get method to call
        var methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                         ?? throw new ArgumentException($"Could not get method '{methodName}'.");

        var genericMethod = methodInfo.MakeGenericMethod(commandType);
       
        // get reference to this
        var instance = Expression.Constant(this);

        // get parameters
        var commandParameter = Expression.Parameter(typeof(object), "command");
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
        
        // and convert the System.Object input to commandType
        var commandConversion = Expression.Convert(commandParameter, commandType);
        
        // build the call
        var call = Expression.Call(instance, genericMethod, commandConversion, cancellationTokenParameter);
        
        // and wrap it in a lambda with a signature we can use
        var lambda = Expression.Lambda<Func<object, CancellationToken, Task>>(call, commandParameter, cancellationTokenParameter);

        return lambda.Compile();
    }
}
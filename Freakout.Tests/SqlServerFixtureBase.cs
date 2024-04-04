﻿using System;
using Nito.AsyncEx.Synchronous;
using Nito.Disposables;
using NUnit.Framework;
using Testcontainers.MsSql;
using Testy;

namespace Freakout.Tests;

[SetUpFixture]
public abstract class SqlServerFixtureBase : FixtureBase
{
    static readonly CollectionDisposable Disposables = new();

    static readonly Lazy<string> LazyConnectionString = new(() =>
    {
        var builder = new MsSqlBuilder();
        var container = builder.Build();

        container.StartAsync().WaitAndUnwrapException();

        Disposables.Add(new Disposable(() => container.DisposeAsync()));

        return container.GetConnectionString();
    });

    protected string ConnectionString => LazyConnectionString.Value;

    [OneTimeTearDown]
    public void CleanUp() => Disposables.Dispose();
}
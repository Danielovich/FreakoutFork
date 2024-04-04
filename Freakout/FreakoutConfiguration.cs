﻿using System;
using Microsoft.Extensions.DependencyInjection;

namespace Freakout;

public abstract class FreakoutConfiguration
{
    public abstract void ConfigureServices(IServiceCollection services);
    
    public TimeSpan OutboxPollInterval { get; set; } = TimeSpan.FromMinutes(1);
}
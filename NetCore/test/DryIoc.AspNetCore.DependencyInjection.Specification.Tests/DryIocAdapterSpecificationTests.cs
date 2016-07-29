﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace DryIoc.AspNetCore.DependencyInjection.Specification.Tests
{
    public class DryIocAdapterSpecificationTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            return new Container().WithDependencyInjectionAdapter(serviceCollection).Resolve<IServiceProvider>();
        }
    }
}
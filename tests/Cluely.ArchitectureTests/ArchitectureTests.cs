using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;
using Xunit.Abstractions;

namespace Cluely.ArchitectureTests;

public class ArchitectureTests(ITestOutputHelper testOutputHelper)
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Common.DomainException).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.Common.ApplicationException).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Configuration.ServiceCollectionExtensions).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Other_Projects()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll([ApplicationAssembly.GetName().Name!, InfrastructureAssembly.GetName().Name!, ApiAssembly.GetName().Name!])
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_AspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Api_Should_Not_Reference_Domain_Directly()
    {
        var result = Types.InAssembly(ApiAssembly)
            .ShouldNot()
            .HaveDependencyOn(DomainAssembly.GetName().Name!)
            .GetResult();
        
        foreach (var type in result.FailingTypes ?? [])
        {
            testOutputHelper.WriteLine($"Failing type: {type.FullName}");
        }

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_Should_Not_Reference_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Controllers_Should_Not_Contain_Business_Logic()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Cluely.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn(DomainAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void No_MediatR()
    {
        var result = Types.InAssemblies([DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly])
            .ShouldNot()
            .HaveDependencyOn("MediatR")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void No_AutoMapper()
    {
        var result = Types.InAssemblies([DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly])
            .ShouldNot()
            .HaveDependencyOn("AutoMapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}

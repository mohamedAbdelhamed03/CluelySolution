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
    public void Domain_Should_Not_Have_Dependency_On_EntityFrameworkCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_SqlClient()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Contain_Persistence_Attributes()
    {
        var domainTypes = DomainAssembly.GetTypes();
        var typesWithPersistenceAttributes = domainTypes
            .Where(type => type.GetCustomAttributes(inherit: true)
                .Any(attribute => attribute.GetType().FullName?.StartsWith("System.ComponentModel.DataAnnotations", StringComparison.Ordinal) == true
                    || attribute.GetType().FullName?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true))
            .Select(type => type.FullName)
            .ToList();

        typesWithPersistenceAttributes.Should().BeEmpty();
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
    public void Infrastructure_Should_Reference_Domain_And_Application()
    {
        var referencedAssemblies = InfrastructureAssembly.GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToList();

        referencedAssemblies.Should().Contain(DomainAssembly.GetName().Name);
        referencedAssemblies.Should().Contain(ApplicationAssembly.GetName().Name);
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
    public void DbContext_Should_Not_Contain_Business_Methods()
    {
        var dbContextType = typeof(Infrastructure.Persistence.CluelyDbContext);
        var methods = dbContextType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method is { Name: not "OnModelCreating", IsSpecialName: false })
            .ToList();

        methods.Should().BeEmpty();
    }

    [Fact]
    public void Entity_Configurations_Should_Only_Map_Persistence()
    {
        var configurationTypes = InfrastructureAssembly.GetTypes()
            .Where(type => type.IsClass
                && type.Namespace == "Cluely.Infrastructure.Persistence.Configurations"
                && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IEntityTypeConfiguration", StringComparison.Ordinal)))
            .ToList();

        configurationTypes.Should().NotBeEmpty();

        foreach (var configurationType in configurationTypes)
        {
            var methods = configurationType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(method => method.Name != "Configure")
                .ToList();

            methods.Should().BeEmpty($"configuration type {configurationType.Name} should only contain mapping logic");
        }
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
    public void Hub_Should_Not_Reference_Domain()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("Cluely.Infrastructure.Delivery.Hubs")
            .ShouldNot()
            .HaveDependencyOn(DomainAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Delivery_Contracts_Should_Not_Reference_Domain()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("Cluely.Infrastructure.Delivery.Contracts")
            .ShouldNot()
            .HaveDependencyOn(DomainAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void VisibilityFilter_Should_Be_Only_Delivery_Component_With_Ownership_Filtering()
    {
        var deliveryTypes = InfrastructureAssembly.GetTypes()
            .Where(type => type.Namespace == "Cluely.Infrastructure.Delivery.Visibility"
                && type.Name is not "VisibilityFilter"
                && type is { IsClass: true, IsNested: false, IsAbstract: false })
            .ToList();

        deliveryTypes.Should().BeEmpty();
    }

    [Fact]
    public void ProjectionBuilder_Should_Not_Mutate_Domain()
    {
        var projectionBuilderType = typeof(Infrastructure.Delivery.Projections.ProjectionBuilder);
        var methods = projectionBuilderType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        methods.Should().OnlyContain(method => method.Name == "Build" || method.Name.StartsWith("Map", StringComparison.Ordinal));
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

    [Fact]
    public void Room_Domain_Events_Should_Implement_IRoomDomainEvent()
    {
        var roomEventTypes = DomainAssembly.GetTypes()
            .Where(type => type.Namespace == "Cluely.Domain.Room.Events"
                && !type.IsInterface
                && typeof(Domain.Common.IRoomDomainEvent).IsAssignableFrom(type))
            .ToList();

        roomEventTypes.Should().NotBeEmpty();
        roomEventTypes.Should().OnlyContain(type =>
            typeof(Domain.Common.IRoomDomainEvent).IsAssignableFrom(type));
    }

    [Fact]
    public void ProjectionBuilder_Should_Not_Reference_SignalR()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("Cluely.Infrastructure.Delivery.Projections")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore.SignalR")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void VisibilityFilter_Should_Not_Reference_Hub()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("Cluely.Infrastructure.Delivery.Visibility")
            .ShouldNot()
            .HaveDependencyOn("Cluely.Infrastructure.Delivery.Hubs")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Delivery_Dispatch_Should_Not_Use_Reflection_To_Extract_RoomId()
    {
        var dispatchTypes = InfrastructureAssembly.GetTypes()
            .Where(type => type.Namespace == "Cluely.Infrastructure.Delivery.Dispatch")
            .ToList();

        foreach (var type in dispatchTypes)
        {
            var methodNames = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(method => method.Name)
                .ToList();

            methodNames.Should().NotContain("ExtractRoomId");
        }
    }

    [Fact]
    public void Room_Event_Serializer_Should_Cover_All_Room_Events()
    {
        var roomEventCount = DomainAssembly.GetTypes()
            .Count(type => type.Namespace == "Cluely.Domain.Room.Events"
                && !type.IsInterface
                && typeof(Domain.Common.IRoomDomainEvent).IsAssignableFrom(type));

        var serializedEventCount = typeof(Infrastructure.Persistence.Mappers.EventSourceGenerationContext)
            .GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonSerializableAttribute), inherit: false)
            .Length;

        serializedEventCount.Should().Be(roomEventCount);
    }

    [Fact]
    public void Api_Request_Models_Should_Not_Reference_Domain()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Cluely.Api.Contracts.Requests")
            .ShouldNot()
            .HaveDependencyOn(DomainAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Api_Response_Models_Should_Not_Reference_Domain()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Cluely.Api.Contracts.Responses")
            .ShouldNot()
            .HaveDependencyOn(DomainAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Api_Controllers_Should_Not_Reference_Infrastructure()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Cluely.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Api_Controllers_Should_Not_Use_DbContext()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Cluely.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Api_ProblemDetails_Mapping_Should_Be_Centralized()
    {
        ApiAssembly.GetTypes()
            .Should()
            .Contain(type => type.Name == "ApiResultMapper" && type.Namespace == "Cluely.Api.Infrastructure");
    }

    [Fact]
    public void Domain_Should_Not_Reference_Identity()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Cluely.Infrastructure.Identity")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();

        var identityPortTypes = ApplicationAssembly.GetTypes()
            .Where(type => type.Namespace == "Cluely.Application.Common.Ports.Identity")
            .Select(type => type.Name)
            .ToList();

        var domainTypeNames = DomainAssembly.GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .ToList();

        foreach (var port in identityPortTypes)
        {
            domainTypeNames.Should().NotContain(name => name.Contains(port, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Identity_Infrastructure_Should_Not_Reference_Room_Aggregate()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("Cluely.Infrastructure.Identity")
            .ShouldNot()
            .HaveDependencyOn("Cluely.Domain.Room")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Auth_Handlers_Should_Not_Reference_Room_Aggregate()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("Cluely.Application.Auth")
            .ShouldNot()
            .HaveDependencyOn("Cluely.Domain.Room")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Contain_Password_Or_Cryptography_Types()
    {
        var forbiddenTypeNames = new[]
        {
            "IPasswordHasher",
            "PasswordHasher",
            "JwtSecurityToken",
            "SymmetricSecurityKey"
        };

        DomainAssembly.GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Should()
            .NotContain(name => forbiddenTypeNames.Any(forbidden =>
                name.Contains(forbidden, StringComparison.Ordinal)));
    }

    [Fact]
    public void Jwt_Configuration_Should_Be_Isolated_To_Api_And_Infrastructure()
    {
        var apiJwtTypes = Types.InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("JwtAuthenticationExtensions")
            .GetTypes();

        apiJwtTypes.Should().NotBeEmpty();

        var domainJwt = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("System.IdentityModel.Tokens.Jwt")
            .GetResult();

        domainJwt.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Handlers_Should_Not_Reference_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("Cluely.Application")
            .And()
            .HaveNameEndingWith("Handler")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Identity_DbContext_Should_Not_Contain_Business_Methods()
    {
        var dbContextType = typeof(Infrastructure.Identity.IdentityDbContext);
        var methods = dbContextType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method is { Name: not "OnModelCreating", IsSpecialName: false })
            .ToList();

        methods.Should().BeEmpty();
    }

    [Fact]
    public void Exception_Handling_Should_Map_ParticipantBindingNotFound_To_Forbidden()
    {
        ApplicationAssembly.GetTypes()
            .Should()
            .Contain(type => type.Name == nameof(Application.Common.ParticipantBindingNotFoundException));
    }
}

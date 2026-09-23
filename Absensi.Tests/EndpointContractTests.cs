using System.Reflection;
using Absensi.Controller;
using Absensi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Absensi.Tests;

public class EndpointContractTests
{
    [Theory]
    [InlineData(typeof(PMController), "api/v1/pm")]
    [InlineData(typeof(AnggotaController), "api/v1/anggota")]
    [InlineData(typeof(DevOpsController), "api/v1/devops")]
    public void UserControllers_UseVersionedApiRoutes(Type controllerType, string expectedRoute)
    {
        var routes = controllerType.GetCustomAttributes<RouteAttribute>()
            .Select(r => r.Template)
            .ToArray();
        Assert.Contains(expectedRoute, routes, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(PMController), "api/PM")]
    [InlineData(typeof(AnggotaController), "api/Anggota")]
    [InlineData(typeof(DevOpsController), "api/DevOps")]
    public void UserControllers_KeepLegacyAliases(Type controllerType, string expectedRoute)
    {
        var routes = controllerType.GetCustomAttributes<RouteAttribute>()
            .Select(r => r.Template)
            .ToArray();
        Assert.Contains(expectedRoute, routes);
    }

    [Theory]
    [InlineData(nameof(ProjectAnggotaController.Create))]
    [InlineData(nameof(ProjectAnggotaController.Delete))]
    public void ProjectAnggota_Mutations_RequireAdminOrPm(string methodName)
    {
        var method = typeof(ProjectAnggotaController).GetMethod(methodName);
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(typeof(ProjectAnggotaController).GetCustomAttributes<AuthorizeAttribute>(inherit: true))
            .ToList();

        Assert.Contains(authorize, a =>
            a.Roles is not null &&
            a.Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .OrderBy(r => r)
                .SequenceEqual(new[] { "Admin", "PM" }.OrderBy(r => r)));
    }

    [Fact]
    public void AbsenPulang_RequiresActingUserId()
    {
        var method = typeof(AbsensiService).GetMethod(nameof(AbsensiService.AbsenPulang));
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(int), parameters[0].ParameterType);
        Assert.Equal("userId", parameters[0].Name);
        Assert.Equal(typeof(Absensi.Models.AbsenPulangDTO), parameters[1].ParameterType);
    }

    [Fact]
    public void AbsenMasuk_RequiresActingUserId()
    {
        var method = typeof(AbsensiService).GetMethod(nameof(AbsensiService.AbsenMasuk));
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(int), parameters[0].ParameterType);
        Assert.Equal("idUser", parameters[0].Name);
        Assert.Equal(typeof(Absensi.Models.AbsenMasukDTO), parameters[1].ParameterType);
    }

    [Fact]
    public void ActiveUserService_ExposesStateValidation()
    {
        var method = typeof(ActiveUserService).GetMethod(nameof(ActiveUserService.IsStillActiveAsync));
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(int), parameters[0].ParameterType);
        Assert.Equal("userId", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal("tokenRole", parameters[1].Name);
        Assert.Equal(typeof(System.Threading.CancellationToken), parameters[2].ParameterType);
    }
}

using System.Security.Claims;
using Absensi.Services;
using Xunit;

namespace Absensi.Tests;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void TryGetUserId_ReadsNameIdentifier()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Role, "Admin"),
        ], "test"));

        Assert.True(user.TryGetUserId(out var id));
        Assert.Equal(42, id);
        Assert.Equal("Admin", user.GetRoleName());
    }

    [Fact]
    public void TryGetUserId_FallsBackToSub()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "7"),
            new Claim("role", "DevOps"),
        ], "test"));

        Assert.True(user.TryGetUserId(out var id));
        Assert.Equal(7, id);
        Assert.Equal("DevOps", user.GetRoleName());
    }
}

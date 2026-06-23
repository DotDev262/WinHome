using WinHome.Infrastructure.Helpers;
using Xunit;

namespace WinHome.Tests;

[Collection("SequentialTests")]
public class AdminGuardTests
{
    [Fact]
    public void EnsureAdministrator_DoesNotThrow_WhenAdmin()
    {
        AdminGuard.IsAdministrator = () => true;
        var ex = Record.Exception(() => AdminGuard.EnsureAdministrator());
        Assert.Null(ex);
    }

    [Fact]
    public void EnsureAdministrator_ThrowsUnauthorizedAccessException_WhenNotAdmin()
    {
        AdminGuard.IsAdministrator = () => false;
        var ex = Assert.Throws<UnauthorizedAccessException>(() => AdminGuard.EnsureAdministrator());
        Assert.Contains("Administrative Privileges", ex.Message);
    }

    [Fact]
    public void ResetAdminCheck_RestoresDefaultDelegate()
    {
        AdminGuard.IsAdministrator = () => false;
        AdminGuard.ResetAdminCheck();
        Assert.NotNull(AdminGuard.IsAdministrator);
    }
}
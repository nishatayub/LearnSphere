using System.Security.Claims;
using LearnSphere.Models;
using Microsoft.AspNetCore.Identity;

namespace LearnSphere.Tests
{
    internal static class TestPrincipal
    {
        public static ClaimsPrincipal For(User user)
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName!)
                },
                IdentityConstants.ApplicationScheme);

            return new ClaimsPrincipal(identity);
        }
    }
}

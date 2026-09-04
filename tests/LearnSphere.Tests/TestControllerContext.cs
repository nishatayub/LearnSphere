using LearnSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LearnSphere.Tests
{
    internal static class TestControllerContext
    {
        public static void ActAs(Controller controller, User user)
        {
            var httpContext = new DefaultHttpContext { User = TestPrincipal.For(user) };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LearnSphere.Tests
{
    /// <summary>
    /// Controllers under test read/write TempData (success/error banners) without any
    /// HTTP pipeline behind them, so Controller.TempData needs a provider or it throws.
    /// This one just keeps values in memory for the life of the request.
    /// </summary>
    internal sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}

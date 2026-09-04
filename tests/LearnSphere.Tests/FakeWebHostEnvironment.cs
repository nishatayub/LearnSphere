using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace LearnSphere.Tests
{
    internal sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "learnsphere-tests-wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "LearnSphere.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
    }
}

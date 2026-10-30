using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace DungeonMasterDashboard.Tests.TestSupport;

internal sealed class FakeWebHostEnvironment(string contentRoot) : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = contentRoot;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = contentRoot;
    public string EnvironmentName { get; set; } = "Testing";
}

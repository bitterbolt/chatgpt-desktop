using System.Net;
using System.Reflection;
using Xunit;

namespace ChatGPT.Tests
{
    public class AppOptionsTests
    {
        [Fact]
        public void Services_ShouldContainConfiguredServices()
        {
            Assert.NotNull(AppOptions.Services);
            Assert.NotEmpty(AppOptions.Services);
            Assert.True(AppOptions.Services.Length >= 6);
        }

        [Fact]
        public void Services_AllEntriesShouldHaveValidNamesAndUrls()
        {
            foreach (var (name, url, iconResource) in AppOptions.Services)
            {
                Assert.False(string.IsNullOrWhiteSpace(name));
                Assert.False(string.IsNullOrWhiteSpace(url));
                Assert.False(string.IsNullOrWhiteSpace(iconResource));
                Assert.True(Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult), $"Invalid URI: {url}");
                Assert.True(uriResult!.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }
        }

        [Fact]
        public void Services_AllIconsMustExistInManifestResources()
        {
            Assembly appAssembly = typeof(AppOptions).Assembly;
            string[] manifestResources = appAssembly.GetManifestResourceNames();

            foreach (var (name, _, iconResource) in AppOptions.Services)
            {
                Assert.Contains(manifestResources, r => string.Equals(r, iconResource, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public void DnsConfiguration_ShouldBeValidIpAddresses()
        {
            Assert.True(IPAddress.TryParse(AppOptions.PrimaryDns, out _), "Primary DNS is invalid IP");
            Assert.True(IPAddress.TryParse(AppOptions.SecondaryDns, out _), "Secondary DNS is invalid IP");
        }

        [Fact]
        public void Timeouts_ShouldBePositive()
        {
            Assert.True(AppOptions.WebViewInitTimeoutMs > 0);
        }
    }
}

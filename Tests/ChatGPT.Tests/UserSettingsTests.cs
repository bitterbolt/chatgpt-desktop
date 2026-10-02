using Xunit;

namespace ChatGPT.Tests
{
    public class UserSettingsTests
    {
        [Fact]
        public void DefaultSettings_ShouldHaveExpectedDefaults()
        {
            var settings = new UserSettings();

            Assert.False(settings.RememberLast);
            Assert.False(settings.RememberWindowSize);
            Assert.False(settings.BypassOnStartup);
            Assert.Null(settings.LastUrl);
            Assert.Equal(0, settings.WindowWidth);
            Assert.Equal(0, settings.WindowHeight);
        }

        [Fact]
        public void Serialization_RoundTrip_PreservesAllProperties()
        {
            var original = new UserSettings
            {
                RememberLast = true,
                RememberWindowSize = true,
                BypassOnStartup = true,
                LastUrl = "https://chat.openai.com",
                WindowWidth = 1280,
                WindowHeight = 800
            };

            string json = original.ToJson();
            Assert.False(string.IsNullOrWhiteSpace(json));

            var deserialized = UserSettings.FromJson(json);
            Assert.NotNull(deserialized);
            Assert.True(deserialized.RememberLast);
            Assert.True(deserialized.RememberWindowSize);
            Assert.True(deserialized.BypassOnStartup);
            Assert.Equal("https://chat.openai.com", deserialized.LastUrl);
            Assert.Equal(1280, deserialized.WindowWidth);
            Assert.Equal(800, deserialized.WindowHeight);
        }

        [Fact]
        public void FromJson_InvalidOrEmpty_ReturnsDefaultInstance()
        {
            var fromNull = UserSettings.FromJson(null);
            Assert.NotNull(fromNull);
            Assert.False(fromNull.RememberLast);

            var fromEmpty = UserSettings.FromJson("");
            Assert.NotNull(fromEmpty);
            Assert.False(fromEmpty.RememberLast);

            var fromCorrupted = UserSettings.FromJson("{ invalid_json: }}}");
            Assert.NotNull(fromCorrupted);
            Assert.False(fromCorrupted.RememberLast);
        }
    }
}

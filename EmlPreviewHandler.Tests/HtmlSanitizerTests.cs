using System;
using MimeKit;
using Xunit;

namespace EmlPreviewHandler.Tests
{
    public class HtmlSanitizerTests
    {
        [Fact]
        public void MaliciousHtml_StripsScriptsHandlersAndJavascriptLinks_BlocksRemoteImages()
        {
            var message = MimeMessage.Load(Fixture.Path("malicious-html.eml"));

            string sanitized = MessageRenderer.BuildBodyHtml(message, allowRemoteImages: false);

            Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onerror", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onclick", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("evil.example.com", sanitized, StringComparison.OrdinalIgnoreCase);

            // Safe content survives sanitization.
            Assert.Contains("Safe link", sanitized);
            Assert.Contains("https://example.com", sanitized);
            Assert.Contains("Congratulations", sanitized);
        }

        [Theory]
        [InlineData("<img src=\"http://example.com/a.png\">", false, false)]
        [InlineData("<img src=\"data:image/png;base64,AAAA\">", false, true)]
        public void Sanitize_RemoteImagePolicy(string html, bool allowRemote, bool expectSrcPresent)
        {
            string result = HtmlSanitizer.Sanitize(html, allowRemote);
            Assert.Equal(expectSrcPresent, result.Contains("src="));
        }
    }
}

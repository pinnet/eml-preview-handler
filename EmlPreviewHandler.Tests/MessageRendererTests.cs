using MimeKit;
using Xunit;

namespace EmlPreviewHandler.Tests
{
    public class MessageRendererTests
    {
        [Fact]
        public void PlainTextMessage_RendersEncodedTextInPreTag()
        {
            var message = MimeMessage.Load(Fixture.Path("plain-text.eml"));

            string html = MessageRenderer.BuildBodyHtml(message);

            Assert.Contains("<pre", html);
            Assert.Contains("This is a plain text test message", html);
            Assert.Empty(MessageRenderer.GetAttachmentNames(message));
        }

        [Fact]
        public void HtmlMessage_ResolvesCidImageAndListsAttachment()
        {
            var message = MimeMessage.Load(Fixture.Path("html-with-inline-image.eml"));

            string html = MessageRenderer.BuildBodyHtml(message);

            Assert.DoesNotContain("cid:logo123@example.com", html);
            Assert.Contains("data:image/png;base64,", html);

            string[] attachments = MessageRenderer.GetAttachmentNames(message);
            Assert.Equal(new[] { "notes.txt" }, attachments);
        }

        [Fact]
        public void HeaderText_IncludesSubjectFromAndTo()
        {
            var message = MimeMessage.Load(Fixture.Path("plain-text.eml"));

            string header = MessageRenderer.BuildHeaderText(message);

            Assert.Contains("Plain text test message", header);
            Assert.Contains("Alice Example", header);
            Assert.Contains("Bob Example", header);
        }
    }
}

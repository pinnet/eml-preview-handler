using System;
using System.IO;
using System.Linq;
using System.Net;
using MimeKit;

namespace EmlPreviewHandler
{
    /// <summary>
    /// Pure (non-UI) logic for turning a parsed .eml into what the preview surface displays.
    /// Kept separate from <see cref="EmlPreviewControl"/> so it can be unit tested without a
    /// WinForms/WebBrowser host.
    /// </summary>
    public static class MessageRenderer
    {
        public static string BuildHeaderText(MimeMessage message) =>
            $"Subject: {message.Subject}\r\n" +
            $"From: {message.From}\r\n" +
            $"To: {message.To}\r\n" +
            $"Date: {message.Date:f}";

        public static string BuildBodyHtml(MimeMessage message, bool allowRemoteImages = false)
        {
            string html = message.HtmlBody;
            if (!string.IsNullOrWhiteSpace(html))
            {
                html = ResolveInlineImages(message, html);
                return HtmlSanitizer.Sanitize(html, allowRemoteImages);
            }

            string text = message.TextBody ?? string.Empty;
            return "<pre style=\"font-family:'Segoe UI',sans-serif;white-space:pre-wrap;\">" +
                   WebUtility.HtmlEncode(text) + "</pre>";
        }

        public static string[] GetAttachmentNames(MimeMessage message) =>
            message.Attachments
                .OfType<MimePart>()
                .Select(p => p.FileName ?? "attachment")
                .ToArray();

        // Inlines cid: referenced images as data: URIs so HTML mail renders correctly
        // without ever issuing a network request for remote content.
        internal static string ResolveInlineImages(MimeMessage message, string html)
        {
            foreach (var part in message.BodyParts.OfType<MimePart>())
            {
                if (string.IsNullOrEmpty(part.ContentId))
                    continue;

                using (var ms = new MemoryStream())
                {
                    part.Content.DecodeTo(ms);
                    string dataUri = $"data:{part.ContentType.MimeType};base64,{Convert.ToBase64String(ms.ToArray())}";
                    string cid = part.ContentId.Trim('<', '>');
                    html = html.Replace($"cid:{cid}", dataUri);
                }
            }

            return html;
        }
    }
}

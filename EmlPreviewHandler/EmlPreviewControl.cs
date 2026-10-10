using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using MimeKit;

namespace EmlPreviewHandler
{
    /// <summary>WinForms control that renders a single .eml file's headers, body and attachment list.</summary>
    internal sealed class EmlPreviewControl : UserControl
    {
        private readonly Label _headerLabel;
        private readonly WebBrowser _browser;
        private readonly ListBox _attachmentList;

        public EmlPreviewControl(string filePath)
        {
            Dock = DockStyle.Fill;

            _headerLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 90,
                Padding = new Padding(8),
                Font = new Font(FontFamily.GenericSansSerif, 9f),
                BackColor = Color.WhiteSmoke
            };

            _attachmentList = new ListBox
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                IntegralHeight = false
            };

            _browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false,
                ScriptErrorsSuppressed = true,
                AllowNavigation = true // needed so Navigating fires; we police it below
            };
            _browser.Navigating += Browser_Navigating;

            Controls.Add(_browser);
            Controls.Add(_attachmentList);
            Controls.Add(_headerLabel);

            LoadMessage(filePath);
        }

        private void Browser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            // Allow the internal "about:blank" step that DocumentText assignment triggers;
            // divert any real link click to the user's default browser instead of navigating
            // the preview pane in place (defends against local-content / script: URI abuse).
            if (e.Url == null || e.Url.ToString() == "about:blank")
                return;

            e.Cancel = true;

            if (e.Url.Scheme == Uri.UriSchemeHttp || e.Url.Scheme == Uri.UriSchemeHttps)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(e.Url.ToString()) { UseShellExecute = true });
                }
                catch
                {
                    // Ignore - preview surface should never fail hard over a bad link.
                }
            }
        }

        private void LoadMessage(string filePath)
        {
            try
            {
                var message = MimeMessage.Load(filePath);

                _headerLabel.Text =
                    $"Subject: {message.Subject}\r\n" +
                    $"From: {message.From}\r\n" +
                    $"To: {message.To}\r\n" +
                    $"Date: {message.Date:f}";

                string html = message.HtmlBody;
                if (!string.IsNullOrWhiteSpace(html))
                {
                    html = ResolveInlineImages(message, html);
                    _browser.DocumentText = HtmlSanitizer.Sanitize(html, allowRemoteImages: false);
                }
                else
                {
                    string text = message.TextBody ?? string.Empty;
                    _browser.DocumentText =
                        "<pre style=\"font-family:'Segoe UI',sans-serif;white-space:pre-wrap;\">" +
                        WebUtility.HtmlEncode(text) + "</pre>";
                }

                PopulateAttachments(message);
            }
            catch (Exception ex)
            {
                _headerLabel.Text = "Unable to preview this message.";
                _attachmentList.Visible = false;
                _browser.DocumentText = "<pre>" + WebUtility.HtmlEncode(ex.Message) + "</pre>";
            }
        }

        private void PopulateAttachments(MimeMessage message)
        {
            var names = message.Attachments
                .OfType<MimePart>()
                .Select(p => p.FileName ?? "attachment")
                .ToArray();

            if (names.Length == 0)
            {
                _attachmentList.Visible = false;
                return;
            }

            _attachmentList.Items.Add($"Attachments ({names.Length}):");
            _attachmentList.Items.AddRange(names.Cast<object>().ToArray());
        }

        // Inlines cid: referenced images as data: URIs so HTML mail renders correctly
        // without ever issuing a network request for remote content.
        private static string ResolveInlineImages(MimeMessage message, string html)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _browser.Navigating -= Browser_Navigating;
            }
            base.Dispose(disposing);
        }
    }
}

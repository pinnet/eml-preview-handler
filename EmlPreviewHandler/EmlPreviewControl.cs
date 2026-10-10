using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using MimeKit;

namespace EmlPreviewHandler
{
    /// <summary>WinForms control that renders a single .eml file's headers, body and attachment list.</summary>
    internal sealed class EmlPreviewControl : UserControl
    {
        private readonly Label _debugLabel;
        private readonly Label _headerLabel;
        private readonly WebBrowser _browser;
        private readonly ListBox _attachmentList;

        public EmlPreviewControl(string filePath)
        {
            Dock = DockStyle.Fill;

            // Always-visible marker so "nothing shows up" can be diagnosed as a registration/COM
            // activation problem (this never appears) vs. a rendering problem (this appears, the
            // rest doesn't). Remove once the handler is confirmed working end to end.
            _debugLabel = new Label
            {
                Location = new Point(0, 0),
                AutoSize = true,
                Text = "DEBUG: EmlPreviewHandler loaded",
                BackColor = Color.Yellow,
                ForeColor = Color.Black,
                Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold)
            };

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
            Controls.Add(_debugLabel);
            _debugLabel.BringToFront();

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

                _headerLabel.Text = MessageRenderer.BuildHeaderText(message);
                _browser.DocumentText = MessageRenderer.BuildBodyHtml(message);
                PopulateAttachments(MessageRenderer.GetAttachmentNames(message));
            }
            catch (Exception ex)
            {
                _headerLabel.Text = "Unable to preview this message.";
                _attachmentList.Visible = false;
                _browser.DocumentText = "<pre>" + WebUtility.HtmlEncode(ex.Message) + "</pre>";
            }
        }

        private void PopulateAttachments(string[] names)
        {
            if (names.Length == 0)
            {
                _attachmentList.Visible = false;
                return;
            }

            _attachmentList.Items.Add($"Attachments ({names.Length}):");
            _attachmentList.Items.AddRange(names.Cast<object>().ToArray());
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

using System;
using System.Linq;
using HtmlAgilityPack;

namespace EmlPreviewHandler
{
    /// <summary>
    /// Minimal allow-list sanitizer for HTML email bodies, which are untrusted,
    /// attacker-controlled content (phishing HTML, tracking pixels, script injection).
    /// Strips active content, event handlers and javascript: URIs, and optionally
    /// blocks remote image loading to avoid read-receipt-style tracking beacons.
    /// </summary>
    public static class HtmlSanitizer
    {
        private static readonly string[] DisallowedTags =
        {
            "script", "style", "iframe", "object", "embed", "link", "meta", "form",
            "input", "button", "applet", "frame", "frameset", "base", "video", "audio", "source"
        };

        public static string Sanitize(string html, bool allowRemoteImages)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            RemoveDisallowedTags(doc.DocumentNode);
            StripDangerousAttributes(doc.DocumentNode, allowRemoteImages);

            return doc.DocumentNode.OuterHtml;
        }

        private static void RemoveDisallowedTags(HtmlNode root)
        {
            var toRemove = root.Descendants()
                .Where(n => DisallowedTags.Contains(n.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var node in toRemove)
                node.Remove();
        }

        private static void StripDangerousAttributes(HtmlNode root, bool allowRemoteImages)
        {
            foreach (var node in root.Descendants().ToList())
            {
                foreach (var attr in node.Attributes.ToList())
                {
                    string name = attr.Name.ToLowerInvariant();
                    string value = attr.Value ?? string.Empty;

                    // Inline event handlers (onclick, onerror, onload, ...).
                    if (name.StartsWith("on", StringComparison.Ordinal))
                    {
                        node.Attributes.Remove(attr);
                        continue;
                    }

                    // javascript: / vbscript: URIs in any href/src-like attribute.
                    string trimmed = value.TrimStart();
                    if ((name == "href" || name == "src" || name == "background" || name == "action") &&
                        (trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase)))
                    {
                        node.Attributes.Remove(attr);
                        continue;
                    }

                    // Remote images are blocked by default (tracking-pixel defense); cid: images
                    // are resolved to data: URIs before sanitization so they survive this check.
                    if (name == "src" && node.Name.Equals("img", StringComparison.OrdinalIgnoreCase) &&
                        !allowRemoteImages && !value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        node.Attributes.Remove(attr);
                    }

                    // Links should never navigate the preview surface itself.
                    if (name == "href" && node.Name.Equals("a", StringComparison.OrdinalIgnoreCase))
                    {
                        node.SetAttributeValue("target", "_blank");
                        node.SetAttributeValue("rel", "noopener noreferrer");
                    }
                }
            }
        }
    }
}

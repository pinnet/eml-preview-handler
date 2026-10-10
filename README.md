# EML Preview Handler

A Windows Explorer [preview handler](https://learn.microsoft.com/en-us/windows/win32/shell/preview-handlers)
that renders `.eml` files (headers, HTML/plain-text body, attachment list)
directly in the File Explorer preview pane — no need to open Outlook or any
other mail client just to glance at a message.

![preview pane](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-blue)
![license](https://img.shields.io/badge/license-MIT-green)

## Features

- Renders the subject/from/to/date headers plus the message body.
- Prefers the HTML body when present, falling back to plain text.
- Resolves `cid:` inline images (e.g. embedded logos) without any network
  access.
- Lists attachment file names.
- Sanitizes untrusted HTML mail content before rendering (see
  [Security](#security) below).

## How it works

Explorer loads preview handlers as COM objects into an isolated surrogate
process (`prevhost.exe`) and asks them to host a child window inside the
preview pane. This project implements that contract in C#:

| File | Role |
|---|---|
| [`Interop/ShellInterop.cs`](EmlPreviewHandler/Interop/ShellInterop.cs) | Hand-declared COM interfaces (`IPreviewHandler`, `IPreviewHandlerVisuals`, `IObjectWithSite`, `IOleWindow`, `IInitializeWithFile`/`IInitializeWithStream`) — there are no built-in .NET wrappers for these. |
| [`PreviewHandlerBase.cs`](EmlPreviewHandler/PreviewHandlerBase.cs) | Reusable COM plumbing: window hosting/sizing, focus, accelerator forwarding, materializing `IStream` input to a temp file. |
| [`EmlPreviewControl.cs`](EmlPreviewHandler/EmlPreviewControl.cs) | Hosts a `WebBrowser` control and wires up link/navigation policing. |
| [`MessageRenderer.cs`](EmlPreviewHandler/MessageRenderer.cs) | Parses the `.eml` with [MimeKit](https://github.com/jstedfast/MimeKit) and builds the header text, body HTML and attachment list — kept UI-free so it's unit testable. |
| [`HtmlSanitizer.cs`](EmlPreviewHandler/HtmlSanitizer.cs) | Allow-list sanitizer for the HTML body. |
| [`EmlPreviewHandler.cs`](EmlPreviewHandler/EmlPreviewHandler.cs) | The concrete COM class, plus `[ComRegisterFunction]`/`[ComUnregisterFunction]` hooks that add the registry entries Explorer requires. |

## Security

Email HTML is untrusted, attacker-controlled content — effectively the same
threat model as rendering arbitrary web pages. `HtmlSanitizer` mitigates the
obvious risks before anything reaches the `WebBrowser` control:

- Strips `<script>`, `<style>`, `<iframe>`, `<object>`, `<embed>`, `<link>`,
  `<meta>`, `<form>` and other active/embedding elements.
- Removes inline event handler attributes (`onclick`, `onerror`, `onload`, ...).
- Removes `javascript:` / `vbscript:` URIs from `href`/`src`/`background`/`action`.
- **Blocks remote image loading by default** (classic tracking-pixel /
  read-receipt defense — the same thing Outlook does). `cid:` inline images
  are resolved to local `data:` URIs first, so legitimate inline images still
  render.
- Link clicks are intercepted and opened in the OS default browser instead of
  navigating the preview pane in place.

This is a pragmatic allow-list, not a formal security boundary — treat it as
defense in depth, not a guarantee against a determined attacker.

## Requirements

- Windows 10/11, x64 (Explorer's `prevhost.exe` is 64-bit; the handler is
  built x64-only to match).
- .NET Framework 4.8 runtime (preinstalled on current Windows).
- .NET SDK to build, and the .NET Framework 4.x RegAsm tool (ships with
  Windows) to register.

## Build

```powershell
cd EmlPreviewHandler
dotnet build -c Release
```

## Register

From an **elevated** PowerShell prompt:

```powershell
cd EmlPreviewHandler
.\register.ps1
```

This builds (if needed), runs `RegAsm.exe /codebase` to register the COM
class, writes the four registry entries Explorer requires (CLSID + AppID,
`.eml\shellex` association, approved `PreviewHandlers` list, `Apartment`
threading model), and sets IE11 "Edge" document-mode emulation for
`prevhost.exe` so HTML renders with modern CSS support instead of IE7 quirks
mode.

Then in File Explorer: **Folder Options → View → "Show preview handlers in
preview pane"** must be enabled, and the preview pane itself toggled on
(**View → Preview pane**, or **Alt+P**).

> **Note:** files downloaded from the internet carry a Mark-of-the-Web tag.
> Since the October 2025 Windows 11 25H2 security update, such files are
> blocked from previewing by default — unblock the file (Properties →
> Unblock, or `Unblock-File`) to preview it anyway.

## Unregister

```powershell
cd EmlPreviewHandler
.\unregister.ps1
```

## Tests

[`EmlPreviewHandler.Tests`](EmlPreviewHandler.Tests) unit tests the non-UI
rendering pipeline (`MessageRenderer` + `HtmlSanitizer`) against sample `.eml`
fixtures in [`Fixtures/`](EmlPreviewHandler.Tests/Fixtures), covering a
plain-text message, an HTML message with an inline `cid:` image and an
attachment, and a message with malicious HTML (script tags, event handlers,
`javascript:` links, a remote tracking image) to confirm sanitization holds.

```powershell
cd EmlPreviewHandler.Tests
dotnet test
```

The `WebBrowser`-hosting UI itself (`EmlPreviewControl`/`PreviewHandlerBase`)
isn't covered here, since it requires a live STA message loop and an actual
Explorer/`prevhost.exe` host to exercise meaningfully — verify that path by
registering the handler and previewing the fixture `.eml` files in Explorer.

## Before deploying beyond your own machine

The CLSID in [`EmlPreviewHandler.cs`](EmlPreviewHandler/EmlPreviewHandler.cs)
is just a random, public identifier (it ends up in the registry in plaintext
regardless, so there's nothing to keep secret). If you fork this project,
regenerate it (`[guid]::NewGuid()` in PowerShell) so your build doesn't
collide with anyone else's registration of the same CLSID.

## Project layout

```
EmlPreviewHandler/
  EmlPreviewHandler.csproj   Project file (net48, x64)
  AssemblyInfo.cs
  Interop/                  Hand-written COM interop declarations
  PreviewHandlerBase.cs      Reusable IPreviewHandler COM base class
  MessageRenderer.cs         UI-free .eml -> header/body/attachments logic
  HtmlSanitizer.cs           HTML allow-list sanitizer
  EmlPreviewControl.cs       WinForms control that renders a message
  EmlPreviewHandler.cs       Concrete handler + COM (un)registration
  register.ps1 / unregister.ps1
EmlPreviewHandler.Tests/     xUnit tests + sample .eml fixtures
preview pane.txt             Research notes this project was built from
```

## References

- [Preview Handlers (Win32 Shell docs)](https://learn.microsoft.com/en-us/windows/win32/shell/preview-handlers)
- [How to Register a Preview Handler](https://learn.microsoft.com/en-us/windows/win32/shell/how-to-register-a-preview-handler)
- [MimeKit](https://github.com/jstedfast/MimeKit)
- [HtmlAgilityPack](https://html-agility-pack.net/)

## License

[MIT](LICENSE)

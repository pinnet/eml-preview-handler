using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EmlPreviewHandler
{
    [ComVisible(true)]
    // Regenerate this GUID (Tools > Create GUID, or [guid]::NewGuid() in PowerShell)
    // before you ship this anywhere beyond your own machine.
    [Guid("6E2F9F2E-9F3E-4F79-8D3B-6E6D1B9E7B1A")]
    [ProgId("EmlPreviewHandler.EmlPreviewHandler")]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class EmlPreviewHandlerControl : PreviewHandlerBase
    {
        private const string FileExtension = ".eml";

        // Well-known, fixed per Microsoft's preview handler documentation - do not change.
        private const string PreviewHandlerShellexGuid = "8895b1c6-b41f-4c1c-a562-0d564250836f";
        private const string PrevhostAppId = "{6d2b5079-2f0b-48dd-ab7f-97cec514d30b}";

        protected override Control CreatePreviewControl(string filePath) => new EmlPreviewControl(filePath);

        [ComRegisterFunction]
        private static void Register(Type type)
        {
            string clsid = type.GUID.ToString("B").ToUpperInvariant();

            using (var inprocKey = Registry.ClassesRoot.OpenSubKey($@"CLSID\{clsid}\InprocServer32", writable: true))
            {
                // Must run apartment-threaded: the handler owns a WinForms/WebBrowser control.
                inprocKey?.SetValue("ThreadingModel", "Apartment");
            }

            using (var clsidKey = Registry.ClassesRoot.OpenSubKey($@"CLSID\{clsid}", writable: true))
            {
                // Tells Explorer to load us into the isolated prevhost.exe surrogate process.
                clsidKey?.SetValue("AppID", PrevhostAppId);
            }

            using (var extKey = Registry.ClassesRoot.CreateSubKey($@"{FileExtension}\shellex\{{{PreviewHandlerShellexGuid}}}"))
            {
                extKey?.SetValue(string.Empty, clsid);
            }

            using (var listKey = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\PreviewHandlers"))
            {
                listKey?.SetValue(clsid, "EML Preview Handler");
            }
        }

        [ComUnregisterFunction]
        private static void Unregister(Type type)
        {
            string clsid = type.GUID.ToString("B").ToUpperInvariant();

            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree(
                    $@"{FileExtension}\shellex\{{{PreviewHandlerShellexGuid}}}", throwOnMissingSubKey: false);
            }
            catch
            {
                // best effort - don't block uninstall on a stale/missing key
            }

            using (var listKey = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\PreviewHandlers", writable: true))
            {
                listKey?.DeleteValue(clsid, throwOnMissingValue: false);
            }
        }
    }
}

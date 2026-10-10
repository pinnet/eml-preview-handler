using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EmlPreviewHandler
{
    [ComVisible(true)]
    // Regenerate this GUID ([guid]::NewGuid() in PowerShell) if you fork this
    // project, so your build doesn't collide with anyone else's registration.
    [Guid("B0E1FC04-4391-4E41-A74A-A1F7C92DDED8")]
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

            try
            {
                // RegAsm only removes the InprocServer32/ProgId entries it created itself; our
                // custom AppID value would otherwise leave CLSID\{guid} behind as an orphan.
                Registry.ClassesRoot.DeleteSubKeyTree($@"CLSID\{clsid}", throwOnMissingSubKey: false);
            }
            catch
            {
                // best effort - don't block uninstall on a stale/missing key
            }
        }
    }
}

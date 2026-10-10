using System;
using System.Runtime.InteropServices;

namespace EmlPreviewHandler.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    // Also the registration GUID used for .eml\shellex\{...}, per Microsoft's
    // "How to Register a Preview Handler" documentation.
    [ComImport]
    [Guid("8895b1c6-b41f-4c1c-a562-0d564250836f")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPreviewHandler
    {
        void SetWindow(IntPtr hwnd, ref RECT rect);
        void SetRect(ref RECT rect);
        void DoPreview();
        void Unload();
        void SetFocus();
        void QueryFocus(out IntPtr phwnd);
        [PreserveSig]
        int TranslateAccelerator(ref MSG pmsg);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PREVIEWHANDLERFRAMEINFO
    {
        public IntPtr cAccelEntries;
        public IntPtr hAccel;
    }

    [ComImport]
    [Guid("fec87aaf-35f9-447a-adb7-20234491401a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPreviewHandlerFrame
    {
        void GetWindowContext(out PREVIEWHANDLERFRAMEINFO pInfo);
        [PreserveSig]
        int TranslateAccelerator(ref MSG pmsg);
    }

    [ComImport]
    [Guid("196bf9a5-b346-4ef0-aa1e-5dcdb76768b1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPreviewHandlerVisuals
    {
        void SetBackgroundColor(uint color);
        void SetFocus();
        void SetTextColor(uint color);
    }

    [ComImport]
    [Guid("fc4801a3-2ba9-11cf-a229-00aa003d7352")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IObjectWithSite
    {
        void SetSite([MarshalAs(UnmanagedType.IUnknown)] object pUnkSite);
        [PreserveSig]
        int GetSite(ref Guid riid, out IntPtr ppvSite);
    }

    [ComImport]
    [Guid("00000114-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleWindow
    {
        void GetWindow(out IntPtr phwnd);
        void ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool fEnterMode);
    }

    [ComImport]
    [Guid("b7d14566-0509-4cce-a71f-0a554233bd9b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IInitializeWithFile
    {
        void Initialize([MarshalAs(UnmanagedType.LPWStr)] string pszFilePath, uint grfMode);
    }

    [ComImport]
    [Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IInitializeWithStream
    {
        void Initialize(System.Runtime.InteropServices.ComTypes.IStream pstream, uint grfMode);
    }
}

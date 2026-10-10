using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using EmlPreviewHandler.Interop;

namespace EmlPreviewHandler
{
    /// <summary>
    /// Boilerplate COM plumbing shared by any IPreviewHandler implementation.
    /// Subclasses only need to supply the WinForms control that renders the file.
    /// </summary>
    [ComVisible(true)]
    public abstract class PreviewHandlerBase :
        IPreviewHandler,
        IPreviewHandlerVisuals,
        IObjectWithSite,
        IOleWindow,
        IInitializeWithFile,
        IInitializeWithStream,
        IPersistFile,
        IDisposable
    {
        private const int E_NOTIMPL = unchecked((int)0x80004001);

        private IntPtr _parentHwnd;
        private RECT _rect;
        private Control _previewControl;
        private object _frame;
        private string _filePath;
        private string _tempFilePath;
        private Color _backgroundColor = SystemColors.Window;
        private Color _textColor = SystemColors.WindowText;

        /// <summary>Create the WinForms control that previews <paramref name="filePath"/>.</summary>
        protected abstract Control CreatePreviewControl(string filePath);

        // ---- IInitializeWithFile ----
        public void Initialize(string pszFilePath, uint grfMode)
        {
            _filePath = pszFilePath;
        }

        // ---- IInitializeWithStream ----
        // Explorer may hand us a stream instead of a path (e.g. items without a real file
        // system location); materialize it to a temp file since MimeKit/the control read by path.
        public void Initialize(IStream pstream, uint grfMode)
        {
            CleanupTempFile();
            _tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".eml");

            using (var fileStream = new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[8192];
                IntPtr bytesReadPtr = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    while (true)
                    {
                        pstream.Read(buffer, buffer.Length, bytesReadPtr);
                        int bytesRead = Marshal.ReadInt32(bytesReadPtr);
                        if (bytesRead <= 0) break;
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(bytesReadPtr);
                }
            }

            _filePath = _tempFilePath;
        }

        // ---- IPersistFile (legacy initialization path some hosts still probe for) ----
        public void GetClassID(out Guid pClassID) => pClassID = GetType().GUID;
        public int IsDirty() => 1; // S_FALSE: never dirty, we are read-only
        public void Load(string pszFileName, int dwMode) => _filePath = pszFileName;
        public void Save(string pszFileName, bool fRemember) { /* read-only handler */ }
        public void SaveCompleted(string pszFileName) { /* read-only handler */ }
        public void GetCurFile(out string ppszFileName) => ppszFileName = _filePath;

        // ---- IPreviewHandler ----
        public void SetWindow(IntPtr hwnd, ref RECT rect)
        {
            _parentHwnd = hwnd;
            _rect = rect;
            UpdateBounds();
        }

        public void SetRect(ref RECT rect)
        {
            _rect = rect;
            UpdateBounds();
        }

        public void DoPreview()
        {
            _previewControl?.Dispose();

            try
            {
                _previewControl = CreatePreviewControl(_filePath);
            }
            catch (Exception ex)
            {
                // Never let an exception escape across the COM boundary here - Explorer just
                // renders a blank pane with no diagnostic if DoPreview fails, so show it instead.
                _previewControl = new Label
                {
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    Text = "Preview failed to load:\r\n" + ex,
                    BackColor = Color.White,
                    ForeColor = Color.Red
                };
            }

            _previewControl.BackColor = _backgroundColor;
            _previewControl.ForeColor = _textColor;

            IntPtr handle = _previewControl.Handle; // forces native window creation
            NativeMethods.SetParent(handle, _parentHwnd);
            UpdateBounds();
            _previewControl.Visible = true;
        }

        public void Unload()
        {
            _previewControl?.Dispose();
            _previewControl = null;
            CleanupTempFile();
            _filePath = null;
        }

        public void SetFocus() => _previewControl?.Focus();

        public void QueryFocus(out IntPtr phwnd) => phwnd = NativeMethods.GetFocus();

        public int TranslateAccelerator(ref MSG pmsg)
        {
            if (_frame is IPreviewHandlerFrame frame)
                return frame.TranslateAccelerator(ref pmsg);
            return E_NOTIMPL;
        }

        private void UpdateBounds()
        {
            if (_previewControl == null) return;
            _previewControl.Bounds = Rectangle.FromLTRB(_rect.Left, _rect.Top, _rect.Right, _rect.Bottom);
        }

        // ---- IPreviewHandlerVisuals ----
        public void SetBackgroundColor(uint color)
        {
            _backgroundColor = ColorTranslator.FromWin32(unchecked((int)color));
            if (_previewControl != null) _previewControl.BackColor = _backgroundColor;
        }

        void IPreviewHandlerVisuals.SetFocus() => SetFocus();

        public void SetTextColor(uint color)
        {
            _textColor = ColorTranslator.FromWin32(unchecked((int)color));
            if (_previewControl != null) _previewControl.ForeColor = _textColor;
        }

        // ---- IObjectWithSite ----
        // Explorer sites us with its IPreviewHandlerFrame so we can forward accelerators (e.g. Ctrl+A, Ctrl+C).
        public void SetSite(object pUnkSite) => _frame = pUnkSite;

        public int GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            ppvSite = IntPtr.Zero;
            return E_NOTIMPL;
        }

        // ---- IOleWindow ----
        public void GetWindow(out IntPtr phwnd) => phwnd = _previewControl?.Handle ?? IntPtr.Zero;

        public void ContextSensitiveHelp(bool fEnterMode) { /* not supported */ }

        private void CleanupTempFile()
        {
            if (_tempFilePath == null) return;
            try { File.Delete(_tempFilePath); } catch { /* best effort cleanup */ }
            _tempFilePath = null;
        }

        public void Dispose()
        {
            Unload();
            GC.SuppressFinalize(this);
        }
    }
}

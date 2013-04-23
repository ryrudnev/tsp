using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace tsp
{
    /// <summary>
    /// Логика взаимодействия для WaitingWindow.xaml
    /// </summary>
    public partial class WaitingWindow : Window
    {
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        private static extern IntPtr GetSystemMenu(IntPtr hwnd, int revert);

        [DllImport("user32.dll", EntryPoint = "GetMenuItemCount")]
        private static extern int GetMenuItemCount(IntPtr hmenu);

        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        private static extern int RemoveMenu(IntPtr hmenu, int npos, int wflags);

        [DllImport("user32.dll", EntryPoint = "DrawMenuBar")]
        private static extern int DrawMenuBar(IntPtr hwnd);

        private const int MF_BYPOSITION = 0x0400;
        private const int MF_DISABLED = 0x0002;

        public WaitingWindow()
        {
            try
            {
                InitializeComponent();
                this.SourceInitialized += new EventHandler(WaitingWindowSourceInitialized);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }  
        }

        void WaitingWindowSourceInitialized(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);

            IntPtr windowHandle = helper.Handle; 
            IntPtr hmenu = GetSystemMenu(windowHandle, 0);
            int cnt = GetMenuItemCount(hmenu);
            RemoveMenu(hmenu, cnt - 1, MF_DISABLED | MF_BYPOSITION);
            RemoveMenu(hmenu, cnt - 2, MF_DISABLED | MF_BYPOSITION);
            DrawMenuBar(windowHandle);
        }
    }
}

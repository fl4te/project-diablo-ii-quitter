using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace ProcessTerminator
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        private Process? selectedProcess; // Nullable
        private Key keyToQuit = Key.None;
        private bool isSettingKey = false;

        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(Window_KeyDown);
            SearchAndSelectGameProcess();
        }

        private void SearchAndSelectGameProcess()
        {
            var gameProcess = Process.GetProcessesByName("Game").FirstOrDefault();
            if (gameProcess != null)
            {
                selectedProcess = gameProcess;
                MessageBox.Show("Project Diablo II process found and selected.");
            }
            else
            {
                selectedProcess = null; // Explicitly set to null to avoid non-nullable warning
                MessageBox.Show("Process not found, please launch Project Diablo II.");
            }
        }

        private void SetQuitKeyButton_Click(object sender, RoutedEventArgs e)
        {
            isSettingKey = true;
            MessageBox.Show("Press the desired key to set as the quit key after clicking OK.");
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isSettingKey)
            {
                keyToQuit = e.Key;
                SelectedKeyTextBlock.Text = $"Selected Key: {keyToQuit}";
                isSettingKey = false;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProcess == null || keyToQuit == Key.None)
            {
                MessageBox.Show("Please ensure Project Diablo II is running and set a key to quit the process.");
                return;
            }

            _proc = HookCallback;
            _hookID = SetHook(_proc);
            MessageBox.Show("Monitoring your Project Diablo II instance started.");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            SearchAndSelectGameProcess();
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)0x0100) // WM_KEYDOWN
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (KeyInterop.KeyFromVirtualKey(vkCode) == keyToQuit)
                {
                    selectedProcess?.Kill();
                    MessageBox.Show("Project Diablo II terminated.");
                    UnhookWindowsHookEx(_hookID);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}

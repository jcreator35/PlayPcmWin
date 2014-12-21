using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace PlayPcmWin {
    public class InterceptMediaKeys : IDisposable {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYUP       = 0x0101;
        private LowLevelKeyboardProc  mCallback      = null;
        private MediaKeyCallbackAsync mCallbackAsync = null;
        private IntPtr mHookId = IntPtr.Zero;

        public InterceptMediaKeys() {
            Init();
        }

        public class MediaKeyEventArgs : EventArgs {
            public Key Key;

            public MediaKeyEventArgs(Key key) {
                Key = key;
            }
        }

        public delegate void MediaKeyEventHandler(object sender, MediaKeyEventArgs args);

        public event MediaKeyEventHandler KeyUp;

        public void Dispose() {
            Term();
        }

        private void Init() {
            mCallback      = CallbackLowLevelKey;
            mCallbackAsync = new MediaKeyCallbackAsync(CallbackMediaKeyAsync);
            mHookId = SetHook(mCallback);
        }

        private void Term() {
            UnhookWindowsHookEx(mHookId);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc) {
            using (Process curProcess = Process.GetCurrentProcess()) {
                using (ProcessModule curModule = curProcess.MainModule) {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private delegate void MediaKeyCallbackAsync(Key key);
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr CallbackLowLevelKey(int nCode, IntPtr pwParam, IntPtr plParam) {
            int wParam = pwParam.ToInt32();
            int lParam = Marshal.ReadInt32(plParam);

            if (nCode >= 0 && wParam == WM_KEYUP) {
                var key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(lParam);
                switch (key) {
                case Key.MediaPlayPause:
                case Key.MediaStop:
                case Key.MediaNextTrack:
                case Key.MediaPreviousTrack:
                    mCallbackAsync.BeginInvoke(key, null, null);
                    break;
                default:
                    break;
                }
            }

            return CallNextHookEx(mHookId, nCode, pwParam, plParam);
        }

        private void CallbackMediaKeyAsync(Key key) {
            if (KeyUp != null) {
                KeyUp(this, new MediaKeyEventArgs(key));
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}

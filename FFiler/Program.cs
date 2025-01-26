using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFiler
{
    internal static class Program
    {
        [StructLayout(LayoutKind.Explicit)]
        struct COPYDATASTRUCT32
        {
            [FieldOffset(0)] public UInt32 dwData;
            [FieldOffset(4)] public UInt32 cbData;
            [FieldOffset(8)] public IntPtr lpData;
        }

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        static extern Int32 sendMessage(Int32 hWnd, Int32 Msg, Int32 wParam, ref COPYDATASTRUCT32 lParam);




        private static Mutex mutex;


        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            


            bool createdNew;
            mutex = new Mutex(true, "ffiler@application_mutex", out createdNew);

            try
            {
                // 二重起動をチェック
                if (!createdNew)
                {
                    var windowHandle = new IntPtr(0);
                    foreach (var process in Process.GetProcesses())
                    {
                        if (process.MainWindowTitle == "FFiler Main Window")
                        {
                            windowHandle = process.MainWindowHandle;
                            break;
                        }
                    }
                    if (((int)windowHandle) != 0)
                    {
                        string message = "please create new window";
                        const int WM_COPYDATA = 0x004A;

                        var cds = new COPYDATASTRUCT32();
                        cds.dwData = 0;     // 任意の数値
                        cds.lpData = Marshal.StringToHGlobalAnsi(message);  // 文字列をキャスト
                        cds.cbData = (uint)message.Length + 1;

                        // メッセージを送信
                        sendMessage((int)windowHandle, WM_COPYDATA, 0, ref cds);

                        Marshal.FreeHGlobal(cds.lpData);  // メモリを解放
                    }


                    return;
                }

                Application.Run(new Main());
            }
            finally
            {
                // ミューテックスが取得されている場合にのみリリース
                if (createdNew)
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}

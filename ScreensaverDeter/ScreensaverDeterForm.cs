using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ScreensaverDeter
{
    public partial class ScreensaverDeterForm : Form
    {

        // 機能の開始／停止を制御するフラグ
        Boolean appStart = false;

        // ホットキー関連の定数
        // モディファイアキー
        const int MOD_ALT = 0x0001;
        const int MOD_CONTROL = 0x0002;
        const int MOD_SHIFT = 0x0004;
        const int MOD_WIN = 0x0008;

        // HotKeyのイベントを示すメッセージID
        const int WM_HOTKEY = 0x0312;

        // HotKey登録の際に指定するID
        // 解除の際や、メッセージ処理を行う際に識別する値となります。
        //
        // 0x0000〜0xbfff 内の適当な値を指定.
        const int APP_START_HOTKEY_ID = 0x0001;
        const int APP_STOP_HOTKEY_ID = 0x0002;

        // ホットキーの登録
        [DllImport("user32.dll")]
        extern static int RegisterHotKey(IntPtr HWnd, int ID, int MOD_KEY, int KEY);

        // ホットキーの解除
        [DllImport("user32.dll")]
        extern static int UnregisterHotKey(IntPtr HWnd, int ID);


        // マウスイベント関連の定数
        const int MOUSEEVENTF_MOVED = 0x0001;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        const int screen_length = 0x10000;

        // アンマネージ DLL 対応用 struct 記述宣言
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;  // amount of wheel movement
            public int dwFlags;
            public int time;       // time stamp for the event
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;  // 0 = INPUT_MOUSE(デフォルト), 1 = INPUT_KEYBOARD
            public MOUSEINPUT mi;
        }

        [DllImport("user32.dll")]
        extern static uint SendInput(
            uint nInputs,     // INPUT 構造体の数(イベント数)
            INPUT[] pInputs,  // INPUT 構造体
            int cbSize        // INPUT 構造体のサイズ
        );

        // 初期化処理
        public ScreensaverDeterForm()
        {
            InitializeComponent();

            // ホットキーの登録
            // CTRL + SHIFT + S　で機能開始
            if (RegisterHotKey(this.Handle, APP_START_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (int)Keys.S) == 0)
            {
                MessageBox.Show("「CTRL + SHIFT + S」をホットキーに登録できませんでした。");
                Application.Exit();
                appExit();
            }
            // CTRL + SHIFT + D　で機能停止
            if (RegisterHotKey(this.Handle, APP_STOP_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (int)Keys.D) == 0)
            {
                MessageBox.Show("「CTRL + SHIFT + D」をホットキーに登録できませんでした。");
                Application.Exit();
                appExit();
            }
        }

        // 終了メニュー押下
        private void AppExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            appExit();
        }

        // OSから渡されるメッセージ処理をオーバーライド
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                if ((int)m.WParam == APP_START_HOTKEY_ID)
                {
                    // 機能開始
                    autoMouseRunStart();
                }
                else if ((int)m.WParam == APP_STOP_HOTKEY_ID)
                {

                    // 機能停止
                    autoMouseRunStop();
                }
            }
        }

        // 機能開始
        private async void autoMouseRunStart()
        {
            appStart = true;

            // マウスポインタの位置を取得する
            // X座標を取得する
            int x = System.Windows.Forms.Cursor.Position.X * (65535 / Screen.PrimaryScreen.Bounds.Width);
            // Y座標を取得する
            int y = System.Windows.Forms.Cursor.Position.Y * (65535 / Screen.PrimaryScreen.Bounds.Height);

            // マウス操作イベントの作成
            INPUT[] input = new INPUT[2];
            input[0].mi.dx = x;
            input[0].mi.dy = y;
            input[0].mi.dwFlags = MOUSEEVENTF_MOVED | MOUSEEVENTF_ABSOLUTE;

            input[1].mi.dx = x + 80;
            input[1].mi.dy = y;
            input[1].mi.dwFlags = MOUSEEVENTF_MOVED | MOUSEEVENTF_ABSOLUTE;


            await Task.Run(() =>
            {
                while (appStart)
                {
                    // イベントの実行
                    SendInput(2, input, Marshal.SizeOf(input[0]));

                }
            });
        }

        // 機能停止
        private void autoMouseRunStop()
        {
            appStart = false;
        }

        // アプリケーションの終了
        private void appExit()
        {
            // ホットキーの解除
            UnregisterHotKey(this.Handle, APP_START_HOTKEY_ID);
            UnregisterHotKey(this.Handle, APP_STOP_HOTKEY_ID);

            // アプリ終了
            notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace DunDefReadyUpAnywhere {
    public partial class Form1 : Form {

        public struct persistant {
            public Keys key;
            public int players;
        }

        public async void WriteFile() {
            try {
                StreamWriter sw = new StreamWriter("settings.json", false);
                persistant ps = new persistant() { key = k, players = player_1.Checked ? 1 : player_2.Checked ? 2 : player_3.Checked ? 3 : 4 };
                sw.WriteLine(JsonConvert.SerializeObject(ps));
                sw.Close();
            }
            catch (Exception) {
                label_status.Text = "Error in writing file";
                await Task.Run(() => Thread.Sleep(10000));
                label_status.Text = "Ready";
            }
        }
        public async void ReadFile() {
            try {
                persistant settings = JsonConvert.DeserializeObject<persistant>(File.ReadAllText("settings.json"));
                switch (settings.players) {
                    case 1: player_1.Checked = true; break;
                    case 2: player_2.Checked = true; break;
                    case 3: player_3.Checked = true; break;
                    case 4: player_4.Checked = true; break;
                }
                k = settings.key;
                button_remember.Checked = true;
                
            }
            catch (Exception) {
                k = Keys.NumPad0;
                label_status.Text = "Error in reading file";
                await Task.Run(() => Thread.Sleep(10000));
                label_status.Text = "Ready";
            }
        }

        private KeyHandler keyHandler;
        Keys k;
        public Form1() {
            InitializeComponent();
            if (File.Exists("settings.json")) { ReadFile(); }
            else {
                k = Keys.NumPad0;
            }

            textBox1.TextChanged += (s, e) => { if (button_remember.Checked) { WriteFile(); } };
            player_1.CheckedChanged += (s, e) => { if (button_remember.Checked) { WriteFile(); } };
            player_2.CheckedChanged += (s, e) => { if (button_remember.Checked) { WriteFile(); } };
            player_3.CheckedChanged += (s, e) => { if (button_remember.Checked) { WriteFile(); } };
            player_4.CheckedChanged += (s, e) => { if (button_remember.Checked) { WriteFile(); } };
            button_remember.CheckedChanged += (s, e) => {
                if (button_remember.Checked) { WriteFile(); }
                else { File.Delete("settings.json"); }
            };

            button_ReadyUp.Click += (s, e) => SendReadyUp();

            
            keyHandler = new KeyHandler(k, this);
            keyHandler.Register();

            textBox1.Text = k.ToString();

            textBox1.KeyDown += (s, e) => {
                if (!(e.KeyCode == k)) {
                    k = e.KeyCode;
                    textBox1.Text = k.ToString();
                    keyHandler.Unregiser();
                    keyHandler = new KeyHandler(k, this);
                    keyHandler.Register();
                    button_ReadyUp.Focus();
                }
            };
        }

        #region HotKey
        private void HandleHotkey() {
            Console.WriteLine("SENT");
            SendReadyUp();

        }

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        bool running = false;
        private async void SendReadyUp() {
            if (running) { return; }
            button_ReadyUp.Enabled = false;
            running = true;

            bool found = false;
            int numPlayers = player_1.Checked ? 1 : player_2.Checked ? 2 : player_3.Checked ? 3 : 4;

            const uint WM_KEYDOWN = 0x100;
            //const uint WM_KEYUP = 0x0101;
            const int waitMS = 100;

            //IntPtr hWnd;
            string processName = "DunDefGame";
            Process[] processList = Process.GetProcesses();

            foreach (Process P in processList) {
                
                //Console.WriteLine(P.ProcessName);
                if (P.ProcessName.Equals(processName)) {
                    found = true;
                    Console.WriteLine("FOUND DUNDEF");
                    IntPtr edit = P.MainWindowHandle;

                    PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.F2, IntPtr.Zero);
                    await Task.Run(() => Thread.Sleep(waitMS));
                    PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.G, IntPtr.Zero);
                    if (numPlayers > 1) {
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.F3, IntPtr.Zero);
                        await Task.Run(() => Thread.Sleep(waitMS));
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.G, IntPtr.Zero);
                    }
                    if (numPlayers > 2) {
                        await Task.Run(() => Thread.Sleep(waitMS));
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.F4, IntPtr.Zero);
                        await Task.Run(() => Thread.Sleep(waitMS));
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.G, IntPtr.Zero);
                    }
                    if (numPlayers > 3) {
                        await Task.Run(() => Thread.Sleep(waitMS));
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.F5, IntPtr.Zero);
                        await Task.Run(() => Thread.Sleep(waitMS));
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.G, IntPtr.Zero);
                    }
                    await Task.Run(() => Thread.Sleep(waitMS));
                    PostMessage(edit, WM_KEYDOWN, (IntPtr)Keys.F2, IntPtr.Zero);
                }
            }

            if (found) { label_status.Text = "Successful"; }
            else { label_status.Text = "Error: Dungeon Defenders not found"; }
            await Task.Run(() => Thread.Sleep(waitMS*15));
            label_status.Text = "Ready";
            button_ReadyUp.Enabled = true;
            running = false;
        }



#pragma warning disable

        #region Really just ignore all this

        internal static class Constants {
            //windows message id for hotkey
            public const int WM_HOTKEY_MSG_ID = 0x0312;
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID) {
                HandleHotkey();
            }

            base.WndProc(ref m);
        }

        public sealed class KeyHandler {
            [DllImport("user32.dll")]
            private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

            [DllImport("user32.dll")]
            private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

            private int key;
            private IntPtr hWnd;
            private int id;

            public KeyHandler(Keys key, Form form) {
                this.key = (int)key;
                this.hWnd = form.Handle;
                id = this.GetHashCode();
            }

            public override int GetHashCode() {
                return key ^ hWnd.ToInt32();
            }

            public bool Register() {
                return RegisterHotKey(hWnd, id, 0, key);
            }

            public bool Unregiser() {
                return UnregisterHotKey(hWnd, id);
            }

        }
        #endregion

        #endregion

    }
}

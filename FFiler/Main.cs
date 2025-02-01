using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFiler
{
    public partial class Main : Form
    {
        const string CREATE_WINDOW = "please create new window";

        [StructLayout(LayoutKind.Explicit)]
        struct COPYDATASTRUCT32
        {
            [FieldOffset(0)] public UInt32 dwData;
            [FieldOffset(4)] public UInt32 cbData;
            [FieldOffset(8)] public IntPtr lpData;
        }

        // WM_COPYDATAのメッセージID
        const int WM_COPYDATA = 0x004A;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case WM_COPYDATA:
                    var cds = (COPYDATASTRUCT32)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT32));
                    string message = Marshal.PtrToStringAnsi(cds.lpData);
                    if(message == CREATE_WINDOW)
                    {
                        Add(Directory.GetCurrentDirectory());
                    }
                    break;
            }
        }

        public Setting setting;

        public Main()
        {
            InitializeComponent();
            setting = null;


            var strPath = Assembly.GetExecutingAssembly().Location;
            strPath = Path.GetDirectoryName(strPath);
            strPath = Path.Combine(strPath, "setting.toml");

            // 設定ファイルの読み込み
            if (File.Exists(strPath))
            {
                setting = Toml.Load(File.ReadAllText(strPath));
            }
        }



        private void Main_Load(object sender, EventArgs e)
        {
            Guid uuid = Guid.NewGuid();

            var tmp = new Form1(this, uuid.ToString(), Directory.GetCurrentDirectory());
            tmp.Show();
            listBox1.Items.Add(uuid.ToString());
        }

        public void Add(string path)
        {
            Guid uuid = Guid.NewGuid();
            var tmp = new Form1(this, uuid.ToString(), path);
            tmp.Show();
            listBox1.Items.Add(uuid.ToString());
        }
        public void Delete(string guid)
        {
            listBox1.Items.Remove(guid);
            if(listBox1.Items.Count == 0)
            {
                Close();
            }
            GC.Collect();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Add(Directory.GetCurrentDirectory());
        }
    }
}

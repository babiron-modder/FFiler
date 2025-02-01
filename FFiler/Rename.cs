using System;
using System.Windows.Forms;

namespace FFiler
{
    public partial class Rename : Form
    {
        public bool Result = false;
        public string Result_Text = "";

        public Rename(string name)
        {
            InitializeComponent();
            textBox1.Text = name;
            Result_Text = name;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1.PerformClick();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Result = true;
            Result_Text = textBox1.Text;
            Close();
        }
    }
}

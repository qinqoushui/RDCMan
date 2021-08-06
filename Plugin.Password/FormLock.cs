using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Extension;

namespace Plugin.Password
{
    public partial class FormLock : QouShui.DLL.Forms.FormNoBorder
    {
        public IAES AES { get; set; }

        public FormLock()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            this.Shown += FormLock_Shown;
        }

        private void FormLock_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
            AcceptButton = button1;
        }

        int c = 3;
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == AES.Pwd)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show($"还有{c--}次机会");
                if (c <= 0)
                {
                    this.DialogResult = DialogResult.Cancel;
                }
            }
        }
    }
}

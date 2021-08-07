using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Extension;

namespace Plugin.Password
{
    public partial class FormPassword : QouShui.DLL.Forms.FormNoBorder
    {

        public FormPassword()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            this.Shown += FormPassword_Shown;
        }

        private void FormPassword_Shown(object sender, EventArgs e)
        {
            this.Activate();
            this.TopMost = true;
            textBox1.Focus();
            this.AcceptButton = button1;
            this.CancelButton = button2;
        }
        public FormPassword ChangeMode(bool isChangePwd)
        {
            label2.Visible = isChangePwd;
            textBox2.Visible = isChangePwd;
            return this;
        }
        public string Pwd { get; private set; }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show($"密码不能为空");
                return;
            }
            if (textBox2.Visible)
            {
                if (string.IsNullOrWhiteSpace(textBox2.Text))
                {
                    MessageBox.Show($"重复密码不能为空");
                    return;
                }
                if (textBox1.Text == textBox2.Text)
                {
                    Pwd = textBox1.Text.Trim();
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show($"两次密码不相同");
                    return;
                }
            }
            else
            {
                Pwd = textBox1.Text.Trim();
                this.DialogResult = DialogResult.OK;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}

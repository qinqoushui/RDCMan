using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using RdcMan;
using Extension;
using System.Windows.Forms;

namespace Plugin.Password
{
    public class PasswordHelper
    {
        private Timer timerLock;
        public PasswordHelper()
        {

        }

        Form mainForm;

        public void PreLoad(IMainForm MainForm)
        {
            timerLock = new Timer();
            timerLock.Interval = 30000;
            timerLock.Tick += (s, e) =>
            {
                if (!locked && DateTime.Now.Subtract(MainForm.LastMouseMoveTime).TotalMinutes > 15)
                {
                    Lock();
                }
                //else
                //    Console.WriteLine(MainForm.LastMouseMoveTime.ToString("F"));
            };
            timerLock.Start();


            mainForm = MainForm as Form;
            var x = new AES();
            x.SetPassword = () =>
            {
                string tPwd = string.Empty;
                checkPwd(false, (succ, pwd) =>
                 {
                     x.SkipSysEncrypt = succ;
                     if (succ)
                     {
                         tPwd = pwd;
                     }
                     else
                     {
                         tPwd = string.Empty;
                     }
                 });
                return tPwd;
            };
            AESXmlUtil.Instance.AES = x;
            lckFrm.AES = x;
            var menu = MainForm.MainMenuStrip.Items["Tools"] as ToolStripMenuItem;
            menu.DropDownItems.Add("锁定", MenuNames.Tools).Click += (s, e) =>
            {
                Lock();
            };
            menu.DropDownItems.Add("系统加密保存", MenuNames.Tools).Click += (s, e) =>
            {
                //检验原密码

                x.SkipSysEncrypt = false;
                x.Pwd = string.Empty;
                MainForm.OnFileSave();
            };
            menu.DropDownItems.Add("自定义加密保存", MenuNames.Tools).Click += (s, e) =>
            {
                checkPwd(true, (succ, pwd) =>
                 {
                     if (succ)
                     {
                         x.SkipSysEncrypt = true;
                         x.Pwd = pwd;
                         //立即保存
                         MainForm.OnFileSave();
                     }
                 }
                );
            };
            MainForm.FileClosed = () => x.Pwd = string.Empty;//文档关闭后清除密码
            // MessageBox.Show("PreLoad", "Plugin.Password event", MessageBoxButtons.OK ,MessageBoxIcon.Information);
        }

        bool locked = false;

        FormLock lckFrm = new FormLock();
        void Lock()
        {
            locked = true;
            lckFrm.TopMost = false;
            if (lckFrm.ShowDialog(mainForm) == DialogResult.OK)
            {
                locked = false;
                (mainForm as IMainForm).LastMouseMoveTime = DateTime.Now;
            }
            else
            {
                System.Windows.Forms.Application.Exit();
            }
        }

        void checkPwd(bool isChangePassword, Action<bool, string> afterCheck)
        {
            using (var f = new FormPassword().ChangeMode(isChangePassword))
            {
                f.BringToFront();
                f.Activate();
                f.ShowInTaskbar = true;
                f.TopMost = true;
                afterCheck(f.ShowDialog() == DialogResult.OK, f.Pwd);
            }
        }
    }
}

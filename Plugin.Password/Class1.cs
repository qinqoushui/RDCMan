using RdcMan;
using System;
using System.Windows.Forms;
using System.Xml;
using System.ComponentModel.Composition;
using Extension;

namespace Plugin.Password
{
    [Export(typeof(IPlugin))]
    public class PluginPassword : IPlugin
    {
        // https://docs.microsoft.com/zh-cn/windows/win32/termserv/mstscaxnotsafeforscripting
        // https://docs.microsoft.com/zh-cn/windows/win32/termserv/imsrdpclientadvancedsettings-interface
        private Timer timerLock;

        public PluginPassword()
        {

        }
        public void OnContextMenu(System.Windows.Forms.ContextMenuStrip contextMenuStrip, RdcTreeNode node)
        {
            //  MessageBox.Show("OnContextMenu", "Plugin.Password event", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void OnDockServer(ServerBase server)
        {
            // MessageBox.Show("OnDockServer", "Plugin.Password event", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void OnUndockServer(IUndockedServerForm form)
        {
            //  MessageBox.Show("OnUndockServer", "Plugin.Password event", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        Form mainForm;
        public void PostLoad(IPluginContext context)
        {
            var frm = context.MainForm;
            frm.MinutesToIdleTimeout = 1; //无操作，闲置，可以锁定：鼠标无移动，所有客户端都空闲了,目前没有合适的方法获取远程上的鼠标移动事件，只能手工强行锁定
            //timerLock = new Timer();
            //timerLock.Interval = 2000;
            //timerLock.Tick += (s, e) =>
            //{
            //    if (DateTime.Now.Subtract(frm.LastMouseMoveTime).TotalSeconds > 20)
            //    {
            //        if (locked)
            //            return;
            //        timerLock.Stop();
            //        Lock();
            //    }
            //    else
            //        Console.WriteLine(frm.LastMouseMoveTime.ToString("F"));
            //};
            //timerLock.Start();
            // MessageBox.Show("PostLoad", "Plugin.Password event", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void PreLoad(IPluginContext context, XmlNode xmlNode)
        {
            mainForm = context.MainForm as Form;
            var x = new AES();
            x.SetPassword = () =>
              {
                  using (var f = new FormPassword().ChangeMode(false))
                  {
                      f.BringToFront();
                      f.Activate();
                      f.ShowInTaskbar = true;
                      f.TopMost = true;
                      if (f.ShowDialog() == DialogResult.OK)
                          return f.Pwd;
                      else
                          return string.Empty;
                  }
              };
            AESXmlUtil.Instance.AES = x;
            lckFrm.AES = x;
            var menu = context.MainForm.MainMenuStrip.Items["Tools"] as ToolStripMenuItem;
            menu.DropDownItems.Add("锁定", MenuNames.Tools).Click += (s, e) =>
              {
                  Lock();
              };
            menu.DropDownItems.Add("加密保存", MenuNames.Tools).Click += (s, e) =>
            {
                using (var f = new FormPassword().ChangeMode(true))
                {
                    f.BringToFront();
                    f.Activate();
                    f.ShowInTaskbar = true;
                    f.TopMost = true;
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        x.Pwd = f.Pwd;
                        //立即保存
                        context.MainForm.OnFileSave();
                    }
                }
            };
            context.MainForm.FileClosed =()=> x.Pwd = string.Empty;//文档关闭后清除密码
            // MessageBox.Show("PreLoad", "Plugin.Password event", MessageBoxButtons.OK ,MessageBoxIcon.Information);
        }

        public XmlNode SaveSettings()
        {
            // MessageBox.Show("SaveSettings", "Plugin.Password event", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        public void Shutdown()
        {
            //  MessageBox.Show("Shutdown", "Plugin.Password event", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        bool locked = false;

        FormLock lckFrm = new FormLock();
        void Lock()
        {
            locked = true;
            if (lckFrm.ShowDialog(mainForm) == DialogResult.OK)
            {
                locked = false;
            }
            else
            {
                Application.Exit();
            }
        }

    }
}

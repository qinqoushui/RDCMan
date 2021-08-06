using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace QouShui.DLL.Forms
{
    public partial class FormNoBorder : Form
    {
        public FormNoBorder()
        {

            this.FormBorderStyle = FormBorderStyle.None;
            InitializeComponent();
            SetWindowLong( this.Handle , -16,0x80000 | 0x20000);
            AddMoveControl(panel1, false);
            


        }

        /// <summary>
        /// 是否使用按钮来结束对话
        /// </summary>
        /// <param name="isShowDialogButton"></param>
        public FormNoBorder(bool isShowDialogButton)
        {
            try
            {
                this.FormBorderStyle = FormBorderStyle.None;
                InitializeComponent();
                SetWindowLong(this.Handle, -16, 0x80000 | 0x20000);

                if (!isShowDialogButton)
                    Controls.Remove(panel1);
            }
            catch
            {

            }
        }
        
        public new  DialogResult  ShowDialog(IWin32Window owner)
        {
            if (atControl == null)
                return base.ShowDialog(owner);  

            if((atControl!=null) && (atControl.Parent!=null))
                this.Location = atControl.Parent.PointToScreen(atControl.Location);
            //判断是否越出屏幕有效边界
            if (Left<0)
            {
                Left=0;
                Top+=AtControl.Height;
            }
            else
                if (Left + Width > Screen.AllScreens[0].WorkingArea.Left + Screen.AllScreens[0].WorkingArea.Width)
                {
                    Left -= Left + Width - (Screen.AllScreens[0].WorkingArea.Left + Screen.AllScreens[0].WorkingArea.Width);
                    Top += AtControl.Height;
                }
                else
                    if (Top+Height> Screen.AllScreens[0].WorkingArea.Top + Screen.AllScreens[0].WorkingArea.Height )
                    {
                        Top -= Top + Height -( Screen.AllScreens[0].WorkingArea.Top +Screen.AllScreens[0].WorkingArea.Height);
                    }

            return base.ShowDialog(owner);
        }

        

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        static extern IntPtr SetWindowLong(IntPtr  hWnd, int nIndex, int dwNewLong);


        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();


        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wparam, int lparam);
        [DllImport("user32.dll")]
        static extern IntPtr PostMessage(IntPtr hWnd, int msg, int wparam, int lparam);


        private List<Control> canMoveList = null;
        /// <summary>
        ///允许导致窗体移动的控件列表
        /// </summary>
        public List<Control> CanMoveList
        {
            get
            {
                return canMoveList;
            }
            set
            {
                canMoveList = value;
            }
        }
        /// <summary>
        /// 绑定允许导致窗体移动的控件列表
        /// 需要提前设置CanMoveList
        /// </summary>
        public void MoveList()
        {
            if (canMoveList == null)
                return;
            foreach (Control control in canMoveList)
                AddMoveControl(control);
        }

        /// <summary>
        /// 允许导致窗体移动的控件
        /// </summary>
        /// <param name="control"></param>
        public void AddMoveControl(Control control)
        {
            AddMoveControl(control, true);
        }
        public void AddMoveControl(Control control, bool withChild)
        {
            control.MouseDown += new MouseEventHandler(mainForm_MouseDown);
            if (withChild)
                foreach (Control c in control.Controls)
                    c.MouseDown += new MouseEventHandler(mainForm_MouseDown);
        }

        protected void mainForm_MouseDown(object sender, MouseEventArgs e)
        {
           
           ReleaseCapture();
            //使用 SENDMESSAGE将导致当前消息异常，比如TREEVIEW点击选择项目时，因为点击事件中途触发了SENDMESSAGE，导致点击消息失效
           // SendMessage(this.Handle, 0x0112, 0xF010 + 0x0002, 0);
            PostMessage(this.Handle, 0x0112, 0xF010 + 0x0002, 0);
          //  (sender as Control).Focus();
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void buttonX2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }


        private bool hasSelected;
        /// <summary>是否选定了有效项
        /// </summary>
        [System.ComponentModel.Description("是否选定了有效项")]
        public bool HasSelected
        {
            get
            {
                return hasSelected;
            }
            set
            {
                hasSelected = value;
            }
        }

        private System.Windows.Forms.ComboBox sourceComboBox;
        /// <summary>對話將要更改的下拉框
        /// </summary>
        /// <remarks>即时更改功能在调用者中编写</remarks>
        [System.ComponentModel.Description("對話將要更改的下拉框")]
        public System.Windows.Forms.ComboBox SourceComboBox
        {
            get
            {
                return sourceComboBox;
            }
            set
            {
                sourceComboBox = value;
            }
        }



        private Control atControl;
        /// <summary>窗口默認尾隨的控件
        /// </summary>
        [System.ComponentModel.Description("窗口默認尾隨的控件")]
        public Control AtControl
        {
            get
            {
                return atControl;
            }
            set
            {
                atControl = value;
            }
        }





         
        /// <summary>按鈕區的高度
        /// </summary>
        public int ButtonPanelHeight
        {
            get
            {
                return panel1.Height ;
            }
        }




    }
}

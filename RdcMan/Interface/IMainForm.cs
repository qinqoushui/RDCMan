using System;
using System.Windows.Forms;

namespace RdcMan
{
    public interface IMainForm
    {
        MenuStrip MainMenuStrip
        {
            get;
        }

        bool RegisterShortcut(Keys shortcutKey, Action action);

        DateTime LastMouseMoveTime { get; set; }

        int MinutesToIdleTimeout { get; set; }
        void OnFileSave();
        Action FileClosed { get; set; }

    }
}

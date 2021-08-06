using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RdcMan
{
    internal partial class MainForm
    {
        public DateTime LastMouseMoveTime { get; set; }
        public int MinutesToIdleTimeout { get; set; } = 1;

    }
}

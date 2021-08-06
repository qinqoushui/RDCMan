using System;

namespace RdcMan
{
	internal interface IRdpClient
	{
		public event EventHandler OnIdleTimeoutNotification;
		//public bool IsIdle { get; set; }
	}
}

using System.Collections.Generic;
using System.Windows.Forms;

namespace RdcMan
{
	public class ConnectionSettings : SettingsGroup
	{
		internal const string TabName = "��������";

		private static Dictionary<string, SettingProperty> _settingProperties;

		protected override Dictionary<string, SettingProperty> SettingProperties => _settingProperties;

		[Setting("connectToConsole")]
		public BoolSetting ConnectToConsole { get; private set; }

		[Setting("startProgram")]
		public StringSetting StartProgram { get; private set; }

		[Setting("workingDir")]
		public StringSetting WorkingDir { get; private set; }

		[Setting("port", DefaultValue = 3389)]
		public IntSetting Port { get; private set; }

		[Setting("loadBalanceInfo")]
		public StringSetting LoadBalanceInfo { get; private set; }

		/// <summary>
		/// ָ���ͻ����������û����������±������ӵ����ʱ�䳤�ȣ����ӣ��� ���ָ����ʱ���ѹ�����ÿؼ������� IMsTscAxEvents���� OnIdleTimeoutNotification ����
		/// </summary>
		/// <see cref="https://docs.microsoft.com/zh-cn/windows/win32/termserv/imsrdpclientadvancedsettings-minutestoidletimeout"/>
		[Setting("minutesToIdleTimeout",DefaultValue =15)]
		public IntSetting MinutesToIdleTimeout { get; set; }


		static ConnectionSettings()
		{
			typeof(ConnectionSettings).GetSettingProperties(out _settingProperties);
		}

		public ConnectionSettings()
			: base("��������", "connectionSettings")
		{
		}

		public override TabPage CreateTabPage(TabbedSettingsDialog dialog)
		{
			return new ConnectionSettingsTabPage(dialog, this);
		}

		protected override void Copy(RdcTreeNode node)
		{
			Copy(node.ConnectionSettings);
		}
	}
}

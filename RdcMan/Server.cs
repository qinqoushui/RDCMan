using AxMSTSCLib;
using MSTSCLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Win32;

namespace RdcMan
{
	public class Server : ServerBase
	{
		private class DisconnectionReason
		{
			public readonly int Code;

			public readonly string Text;

			public DisconnectionReason(int code, string text)
			{
				Code = code;
				Text = text;
			}
		}

		public const string XmlNodeName = "server";

		internal const string XmlDisplayNameTag = "displayName";

		internal const string XmlServerNameTag = "name";

		internal const string XmlCommentTag = "comment";

		internal const string ConnectionTypeTag = "connectionType";

		internal const string VirtualMachineIdTag = "vmId";

		private const bool SimulateConnections = false;

		private RdpClient.ConnectionState _connectionState;

		private static readonly Dictionary<string, Helpers.ReadXmlDelegate> PropertyActions;

		private RdpClient _client;

		private ServerBox _serverBox;

		private readonly List<ServerRef> _serverRefList;

		private DisplayStates _displayState;

		private string _disconnectionReason = string.Empty;

		private readonly object _connectionStateLock = new object();

		private int _noFullScreenBehavior;

		private static readonly DisconnectionReason[] DisconnectionReasons;

		private static readonly DisconnectionReason[] ExtendedDisconnectionReasons;

		public RdpClient.ConnectionState ConnectionState
		{
			get
			{
				return _connectionState;
			}
			private set
			{
				if (_connectionState != value)
				{
					_connectionState = value;
					Action<ConnectionStateChangedEventArgs> connectionStateChanged = Server.ConnectionStateChanged;
					if (connectionStateChanged != null)
					{
						ConnectionStateChangedEventArgs obj = new ConnectionStateChangedEventArgs(this, _connectionState);
						connectionStateChanged(obj);
					}
				}
			}
		}

		public override Server ServerNode => this;

		public override string RemoveTypeDescription => "server";

		public override DisplayStates DisplayState
		{
			get
			{
				return _displayState;
			}
			set
			{
				if (value != _displayState)
				{
					_displayState = value;
					if (value != 0)
					{
						SetText();
						SetClientSizeProperties();
					}
				}
			}
		}

		public override bool IsClientDocked
		{
			get
			{
				if (IsClientInitialized)
				{
					return ServerForm == null;
				}
				return true;
			}
		}

		public override bool IsClientUndocked
		{
			get
			{
				if (IsClientInitialized)
				{
					return ServerForm != null;
				}
				return false;
			}
		}

		public override RdcBaseForm ParentForm
		{
			get
			{
				if (IsClientUndocked)
				{
					return ServerForm;
				}
				return base.ParentForm;
			}
		}

		private ServerForm ServerForm => Client.Control.Parent as ServerForm;

		public override Size Size
		{
			get
			{
				if (!UseServerBox)
				{
					return _client.Control.Size;
				}
				return _serverBox.Size;
			}
			set
			{
				if (!UseServerBox)
				{
					_client.Control.Size = value;
				}
				_serverBox.Size = value;
			}
		}

		private bool IsClientInPanel
		{
			get
			{
				if (IsClientInitialized)
				{
					return IsClientDocked;
				}
				return false;
			}
		}

		public override Point Location
		{
			get
			{
				if (!UseServerBox)
				{
					return _client.Control.Location;
				}
				return _serverBox.Location;
			}
			set
			{
				if (!UseServerBox)
				{
					_client.Control.Location = value;
				}
				_serverBox.Location = value;
			}
		}

		public string ConnectedText
		{
			get
			{
				if (base.IsThumbnail)
				{
					return "Connected";
				}
				return "Connected to " + GetQualifiedNameForUI();
			}
		}

		public string ConnectingText
		{
			get
			{
				if (base.IsThumbnail)
				{
					return "连接中...";
				}
				return "连接到 " + GetQualifiedNameForUI();
			}
		}

		public string DisconnectedText
		{
			get
			{
				string text;
				if (base.IsThumbnail)
				{
					text = "未连接";
					if (!string.IsNullOrEmpty(_disconnectionReason))
					{
						text += " [error]";
					}
				}
				else
				{
					text = "未连接 " + GetQualifiedNameForUI();
					if (!string.IsNullOrEmpty(_disconnectionReason))
					{
						string text2 = text;
						text = text2 + Environment.NewLine + "[" + _disconnectionReason + "]";
					}
				}
				return text;
			}
		}

		internal RdpClient Client => _client;

		private bool IsClientInitialized => Client != null;

		private bool UseServerBox
		{
			get
			{
				if (IsClientInPanel)
				{
					if (base.IsThumbnail)
					{
						return !(ServerNode.Parent as GroupBase).DisplaySettings.SessionThumbnailPreview.Value;
					}
					return false;
				}
				return true;
			}
		}

		public override bool IsConnected => ConnectionState != RdpClient.ConnectionState.Disconnected;

		public override bool IsClientFullScreen
		{
			get
			{
				if (IsClientInitialized)
				{
					return Client.MsRdpClient.FullScreen;
				}
				return false;
			}
		}

		public static event Action<ConnectionStateChangedEventArgs> ConnectionStateChanged;

		public static event Action<Server> FocusReceived;

		static Server()
		{
			PropertyActions = new Dictionary<string, Helpers.ReadXmlDelegate>
			{
				{
					"name",
					delegate(XmlNode childNode, RdcTreeNode node, ICollection<string> errors)
					{
						(node as Server).Properties.ServerName.Value = childNode.InnerText;
					}
				},
				{
					"connectionType",
					delegate(XmlNode childNode, RdcTreeNode node, ICollection<string> errors)
					{
						Enum.TryParse(childNode.InnerText, out ConnectionType result);
						(node as Server).Properties.ConnectionType.Value = result;
					}
				},
				{
					"vmId",
					delegate(XmlNode childNode, RdcTreeNode node, ICollection<string> errors)
					{
						(node as Server).Properties.VirtualMachineId.Value = childNode.InnerText;
					}
				},
				{
					"displayName",
					delegate(XmlNode childNode, RdcTreeNode node, ICollection<string> errors)
					{
						(node as Server).Properties.DisplayName.Value = childNode.InnerText;
					}
				},
				{
					"comment",
					delegate(XmlNode childNode, RdcTreeNode node, ICollection<string> errors)
					{
						XmlNode firstChild = childNode.FirstChild;
						if (firstChild != null)
						{
							(node as Server).Properties.Comment.Value = childNode.InnerText;
						}
					}
				}
			};
			DisconnectionReasons = new DisconnectionReason[31]
			{
				new DisconnectionReason(1, ""),
				new DisconnectionReason(2, ""),
				new DisconnectionReason(3, ""),
				new DisconnectionReason(260, "DNS名称查找失败"),
				new DisconnectionReason(263, "验证失败"),
				new DisconnectionReason(264, "连接超时"),
				new DisconnectionReason(516, "无法建立连接"),
				new DisconnectionReason(522, "未检测到智能卡读卡器"),
				new DisconnectionReason(1289, "服务器不支持身份验证"),
				new DisconnectionReason(1800, "您已经在进行控制台会话"),
				new DisconnectionReason(2052, "指定的IP地址错误"),
				new DisconnectionReason(2055, "登录失败"),
				new DisconnectionReason(2056, "服务器没有可用的会话"),
				new DisconnectionReason(2308, "套接字已关闭"),
				new DisconnectionReason(2567, "没有指定的用户账号"),
				new DisconnectionReason(2824, "会话被其他客户端连接"),
				new DisconnectionReason(2825, "服务器身份验证失败"),
				new DisconnectionReason(3847, "密码已过期"),
				new DisconnectionReason(4615, "首次登录前必须更改用户密码"),
				new DisconnectionReason(7175, "向智能卡提供了错误的PIN"),
				new DisconnectionReason(7943, "没有提供证书"),
				new DisconnectionReason(8711, "智能卡被阻止"),
				new DisconnectionReason(50331655, "网关身份验证失败"),
				new DisconnectionReason(50331656, "找不到服务器"),
				new DisconnectionReason(50331660, "无法连接到网关"),
				new DisconnectionReason(50331669, "智能卡身份验证失败"),
				new DisconnectionReason(50331670, "找不到服务器"),
				new DisconnectionReason(50331676, "您的用户或计算机帐户无权访问网关服务器"),
				new DisconnectionReason(50331677, "没有提供网关凭证"),
				new DisconnectionReason(50331678, ""),
				new DisconnectionReason(50331686, "未输入智能卡PIN码")
			};
			ExtendedDisconnectionReasons = new DisconnectionReason[26]
			{
				new DisconnectionReason(0, "没有其他可用信息"),
				new DisconnectionReason(1, ""),
				new DisconnectionReason(2, ""),
				new DisconnectionReason(3, "服务器已断开与客户端的连接，因为客户端空闲时间超过了指定的超时时间"),
				new DisconnectionReason(4, "服务器已断开客户端连接，因为客户端已超过指定的连接期限"),
				new DisconnectionReason(5, "客户端的连接已替换为另一个连接"),
				new DisconnectionReason(6, "内存不足"),
				new DisconnectionReason(7, "服务器拒绝连接"),
				new DisconnectionReason(8, "服务器出于安全原因拒绝连接"),
				new DisconnectionReason(9, "用户帐户无权进行远程登录"),
				new DisconnectionReason(10, "必须重新提供用户帐户证书"),
				new DisconnectionReason(11, "客户端已远程断开连接"),
				new DisconnectionReason(12, "连接丢失"),
				new DisconnectionReason(256, "内部许可错误"),
				new DisconnectionReason(257, "没有可用的许可服务器"),
				new DisconnectionReason(258, "没有有效的软件许可证"),
				new DisconnectionReason(259, "远程计算机收到无效的许可消息"),
				new DisconnectionReason(260, "硬件ID与软件许可证上指定的ID不匹配"),
				new DisconnectionReason(261, "客户端许可证错误"),
				new DisconnectionReason(262, "许可协议期间发生网络问题"),
				new DisconnectionReason(263, "客户端过早终止了许可协议"),
				new DisconnectionReason(264, "授权消息未正确加密"),
				new DisconnectionReason(265, "本地计算机的客户端访问许可证无法升级或更新"),
				new DisconnectionReason(266, "远程计算机未获得许可以接受远程连接"),
				new DisconnectionReason(267, ""),
				new DisconnectionReason(768, "")
			};
			ServerTree.Instance.ServerChanged += OnServerChanged;
		}

		protected Server()
		{
			_serverRefList = new List<ServerRef>();
			ChangeImageIndex(ImageConstants.DisconnectedServer);
		}

		private static void OnServerChanged(ServerChangedEventArgs e)
		{
			if (e.ChangeType.HasFlag(ChangeType.PropertyChanged))
			{
				(e.Server as Server)?.VisitServerRefs(delegate(ServerRef r)
				{
					GroupBase group = r.Parent as GroupBase;
					if (ServerTree.Instance.SortGroup(group))
					{
						ServerTree.Instance.OnGroupChanged(group, ChangeType.InvalidateUI);
					}
				});
			}
		}

		public void SuspendFullScreenBehavior()
		{
			Interlocked.Increment(ref _noFullScreenBehavior);
		}

		public void ResumeFullScreenBehavior()
		{
			Interlocked.Decrement(ref _noFullScreenBehavior);
		}

		public string GetQualifiedNameForUI()
		{
			SplitName(base.ServerName, out string serverName, out int _);
			if (base.DisplayName.Equals(serverName, StringComparison.OrdinalIgnoreCase))
			{
				return base.DisplayName;
			}
			return $"{base.DisplayName} ({serverName})";
		}

		private void SetText()
		{
			_serverBox.SetText();
			if (IsClientInitialized)
			{
				_client.SetText();
			}
		}

		public string GetConnectionStateText()
		{
			switch (ConnectionState)
			{
			case RdpClient.ConnectionState.Disconnected:
				return DisconnectedText;
			case RdpClient.ConnectionState.Connecting:
				return ConnectingText;
			case RdpClient.ConnectionState.Connected:
				return ConnectedText;
			default:
				return "<GetText error>";
			}
		}

		protected override void InitSettings()
		{
			base.Properties = new ServerSettings();
			base.DisplaySettings = new ServerDisplaySettings();
			base.InitSettings();
		}

		internal override void Focus()
		{
			if (!IsClientUndocked && UseServerBox)
			{
				_serverBox.Focus();
			}
			else
			{
				_client.Control.Focus();
			}
		}

		internal override void FocusConnectedClient()
		{
			if (IsConnected && IsClientInitialized)
			{
				_client.Control.Focus();
			}
		}

		internal void SetNormalView()
		{
			DisplayState = DisplayStates.Normal;
			Size = Program.TheForm.GetClientSize();
			Location = new Point(0, 0);
			EnableDisableClient();
		}

		internal void SetThumbnailView(int left, int top, int width, int height)
		{
			DisplayState = DisplayStates.Thumbnail;
			Size = new Size(width, height);
			Location = new Point(left, top);
			EnableDisableClient();
		}

		internal override void ScreenCapture()
		{
			Control control = Client.Control;
			Graphics graphics = null;
			try
			{
				Point point = control.PointToScreen(control.Location);
				Size size = control.Size;
				Bitmap bitmap = new Bitmap(size.Width, size.Height);
				graphics = Graphics.FromImage(bitmap);
				graphics.CopyFromScreen(point.X, point.Y, 0, 0, bitmap.Size);
				Clipboard.SetDataObject(bitmap);
			}
			catch (Exception ex)
			{
				FormTools.ErrorDialog("捕获会话屏幕时出错: " + ex.Message);
			}
			finally
			{
				graphics?.Dispose();
			}
		}

		protected void InitRequiredForDisplay()
		{
			_serverBox = new ServerBox(this);
		}

		private void AddToClientPanel()
		{
			if (_serverBox.Parent == null)
			{
				Program.TheForm.AddToClientPanel(_serverBox);
			}
		}

		private void RemoveFromClientPanel()
		{
			if (_serverBox.Parent != null)
			{
				Program.TheForm.RemoveFromClientPanel(_serverBox);
			}
		}

		private void InitClient()
		{
			if (!IsClientInitialized)
			{
				_client = RdpClient.AllocClient(this, Program.TheForm);
				_client.ConnectConnectionHandlers(OnConnected, OnConnecting, OnDisconnected, OnAutoReconnecting, OnAutoReconnecting2, OnAutoReconnected, OnFocusReleased);
				_client.ConnectContainerHandlers(OnRequestGoFullScreen, OnRequestLeaveFullScreen, OnRequestContainerMinimize, OnConfirmClose, OnFatalError);
				_client.Control.GotFocus += ClientGotFocus;
				_client.AdvancedSettings2.ContainerHandledFullScreen = 1;
				_client.AdvancedSettings2.allowBackgroundInput = 1;
				_client.Control.Size = _serverBox.Size;
				_client.Control.Location = _serverBox.Location;
				SetClientSizeProperties();
				SetText();
				if (!UseServerBox && _serverBox.Visible)
				{
					_client.Control.Show();
					_serverBox.Hide();
				}
			}
		}

		private void DestroyClient()
		{
			if (IsClientInitialized)
			{
				_client.DisconnectConnectionHandlers(OnConnected, OnConnecting, OnDisconnected, OnAutoReconnecting, OnAutoReconnecting2, OnAutoReconnected, OnFocusReleased);
				_client.DisconnectContainerHandlers(OnRequestGoFullScreen, OnRequestLeaveFullScreen, OnRequestContainerMinimize, OnConfirmClose, OnFatalError);
				RdpClient client = _client;
				_client = null;
				RdpClient.ReleaseClient(client);
			}
		}

		internal static Server CreateForAddDialog()
		{
			return new Server();
		}

		public static Server Create(string serverName, string displayName, GroupBase group)
		{
			Server server = new Server();
			server.Properties.ServerName.Value = serverName;
			server.Properties.DisplayName.Value = displayName;
			server.FinishConstruction(group);
			return server;
		}

		internal static Server Create(ServerPropertiesDialog dlg)
		{
			Server server = dlg.AssociatedNode as Server;
			server.FinishConstruction(dlg.PropertiesPage.ParentGroup);
			return server;
		}

		internal static Server Create(string name, ServerPropertiesDialog dlg)
		{
			Server node = dlg.AssociatedNode as Server;
			Server server = new Server();
			server.CopySettings(node, null);
			server.Properties.ServerName.Value = name;
			server.Properties.DisplayName.Value = name;
			server.FinishConstruction(dlg.PropertiesPage.ParentGroup);
			return server;
		}

		internal static Server Create(XmlNode xmlNode, GroupBase group, ICollection<string> errors)
		{
			Server server = new Server();
			server.ReadXml(xmlNode, errors);
			server.FinishConstruction(group);
			return server;
		}

		protected void FinishConstruction(GroupBase group)
		{
			if (string.IsNullOrEmpty(base.DisplayName))
			{
				Properties.DisplayName.Value = base.ServerName;
			}
			base.Text = base.DisplayName;
			InitRequiredForDisplay();
			ServerTree.Instance.AddNode(this, group);
		}

		private void ReadXml(XmlNode xmlNode, ICollection<string> errors)
		{
			ReadXml(PropertyActions, xmlNode, errors);
		}

		public override void OnRemoving()
		{
			VisitServerRefs(delegate(ServerRef r)
			{
				r.OnRemoveServer();
			});
			_serverRefList.Clear();
			if (IsClientUndocked)
			{
				ServerForm.Close();
			}
			Hide();
			_serverBox.Dispose();
			_serverBox = null;
			DestroyClient();
		}

		internal override void Show()
		{
			AddToClientPanel();
			if (UseServerBox)
			{
				_serverBox.Show();
			}
			else
			{
				_client.Control.Show();
			}
		}

		internal override void Hide()
		{
			if (DisplayState != 0)
			{
				DisplayState = DisplayStates.Invalid;
				_serverBox.Hide();
				RemoveFromClientPanel();
				if (IsClientInPanel)
				{
					_client.Control.Hide();
				}
			}
		}

		public override void Connect()
		{
			ConnectAs(null, null);
		}

		public override void ConnectAs(LogonCredentials logonCredentials, ConnectionSettings connectionSettings)
		{
			InitClient();
			lock (_connectionStateLock)
			{
				if (!IsConnected)
				{
					InheritSettings();
					ResolveCredentials();
					if (logonCredentials == null)
					{
						logonCredentials = base.LogonCredentials;
					}
					else
					{
						ResolveCredentials(logonCredentials);
					}
					if (connectionSettings == null)
					{
						connectionSettings = base.ConnectionSettings;
					}
					string str = "{none}";
					try
					{
						IMsRdpClientAdvancedSettings advancedSettings = _client.AdvancedSettings2;
						IMsRdpClientAdvancedSettings6 advancedSettings2 = _client.AdvancedSettings7;
						IMsRdpClientAdvancedSettings7 advancedSettings3 = _client.AdvancedSettings8;
						IMsRdpClientNonScriptable4 msRdpClientNonScriptable = (IMsRdpClientNonScriptable4)_client.GetOcx();
						SplitName(base.ServerName, out string serverName, out int port);
						if (port == -1)
						{
							port = base.ConnectionSettings.Port.Value;
						}
						str = "server name";
						_client.MsRdpClient.Server = serverName;
						string userName = CredentialsUI.GetUserName(logonCredentials.UserName.Value);
						string domain = logonCredentials.Domain.Value;
						if (!string.IsNullOrEmpty(userName))
						{
							str = "user name";
							_client.MsRdpClient.UserName = userName;
						}
						else
						{
							_client.MsRdpClient.UserName = null;
						}
						if (!string.IsNullOrEmpty(domain))
						{
							str = "domain";
							if (domain.Equals("[server]", StringComparison.OrdinalIgnoreCase))
							{
								_client.MsRdpClient.Domain = base.ServerName;
							}
							else if (domain.Equals("[display]", StringComparison.OrdinalIgnoreCase))
							{
								_client.MsRdpClient.Domain = base.DisplayName;
							}
							else
							{
								_client.MsRdpClient.Domain = domain;
							}
						}
						else
						{
							_client.MsRdpClient.Domain = null;
						}
						str = "password";
						if (logonCredentials.Password.IsDecrypted && !string.IsNullOrEmpty(logonCredentials.Password.Value))
						{
							advancedSettings.ClearTextPassword = logonCredentials.Password.Value;
						}
						advancedSettings.keepAliveInterval = 60000;
						advancedSettings.MinutesToIdleTimeout =connectionSettings.MinutesToIdleTimeout.Value;
						advancedSettings2.HotKeyAltEsc = (int)Program.Preferences.HotKeyAltEsc;
						advancedSettings2.HotKeyAltSpace = (int)Program.Preferences.HotKeyAltSpace;
						advancedSettings2.HotKeyAltShiftTab = (int)Program.Preferences.HotKeyAltShiftTab;
						advancedSettings2.HotKeyAltTab = (int)Program.Preferences.HotKeyAltTab;
						advancedSettings2.HotKeyCtrlEsc = (int)Program.Preferences.HotKeyCtrlEsc;
						advancedSettings2.HotKeyCtrlAltDel = (int)Program.Preferences.HotKeyCtrlAltDel;
						advancedSettings2.HotKeyFocusReleaseLeft = (int)Program.Preferences.HotKeyFocusReleaseLeft;
						advancedSettings2.HotKeyFocusReleaseRight = (int)Program.Preferences.HotKeyFocusReleaseRight;
						advancedSettings2.HotKeyFullScreen = (int)Program.Preferences.HotKeyFullScreen;
						_client.SecuredSettings2.KeyboardHookMode = (int)base.LocalResourceSettings.KeyboardHookMode.Value;
						RdpClient.ConnectionBarState connectionBarState = Program.Preferences.ConnectionBarState;
						if (connectionBarState == RdpClient.ConnectionBarState.Off)
						{
							advancedSettings.DisplayConnectionBar = false;
						}
						else
						{
							advancedSettings.DisplayConnectionBar = true;
							advancedSettings.PinConnectionBar = (connectionBarState == RdpClient.ConnectionBarState.Pinned);
						}
						_client.MsRdpClient.FullScreen = false;
						advancedSettings.PerformanceFlags = Program.Preferences.PerformanceFlags;
						advancedSettings.GrabFocusOnConnect = false;
						str = "gateway settings";
						ConfigureGateway();
						if (Properties.ConnectionType.Value == ConnectionType.VirtualMachineConsoleConnect)
						{
							advancedSettings3.PCB = Properties.VirtualMachineId.Value;
							advancedSettings.RDPPort = 2179;
							advancedSettings.ConnectToServerConsole = true;
							advancedSettings2.AuthenticationLevel = 0u;
							advancedSettings2.AuthenticationServiceClass = "Microsoft Virtual Console Service";
							advancedSettings2.EnableCredSspSupport = true;
							advancedSettings3.NegotiateSecurityLayer = false;
						}
						else
						{
							str = "port";
							advancedSettings.RDPPort = port;
							str = "loadBalanceInfo";
							string text = base.ConnectionSettings.LoadBalanceInfo.Value;
							if (!string.IsNullOrEmpty(text))
							{
								if (text.Length % 2 == 1)
								{
									text += " ";
								}
								text += Environment.NewLine;
								byte[] bytes = Encoding.UTF8.GetBytes(text);
								advancedSettings.LoadBalanceInfo = Encoding.Unicode.GetString(bytes);
							}
							str = "connect to console";
							if (advancedSettings2 != null)
							{
								advancedSettings2.ConnectToAdministerServer = connectionSettings.ConnectToConsole.Value;
							}
							advancedSettings.ConnectToServerConsole = connectionSettings.ConnectToConsole.Value;
							str = "start program";
							_client.SecuredSettings.StartProgram = base.ConnectionSettings.StartProgram.Value;
							_client.SecuredSettings.WorkDir = base.ConnectionSettings.WorkingDir.Value;
							_client.AdvancedSettings5.EnableAutoReconnect = true;
							_client.AdvancedSettings5.MaxReconnectAttempts = 20;
							advancedSettings.EnableWindowsKey = 1;
							str = "local resources";
							_client.SecuredSettings2.AudioRedirectionMode = (int)base.LocalResourceSettings.AudioRedirectionMode.Value;
							if (advancedSettings3 != null)
							{
								advancedSettings3.AudioQualityMode = (uint)base.LocalResourceSettings.AudioRedirectionQuality.Value;
								advancedSettings3.AudioCaptureRedirectionMode = (base.LocalResourceSettings.AudioCaptureRedirectionMode.Value == RdpClient.AudioCaptureRedirectionMode.Record);
								if (RdpClient.SupportsPanning)
								{
									advancedSettings3.EnableSuperPan = Program.Preferences.EnablePanning;
									advancedSettings3.SuperPanAccelerationFactor = (uint)Program.Preferences.PanningAcceleration;
								}
							}
							if (RdpClient.SupportsFineGrainedRedirection)
							{
								IMsRdpDriveCollection driveCollection = _client.ClientNonScriptable3.DriveCollection;
								for (uint num = 0u; num < driveCollection.DriveCount; num++)
								{
									IMsRdpDrive msRdpDrive = driveCollection.get_DriveByIndex(num);
									string item = msRdpDrive.Name.Substring(0, msRdpDrive.Name.Length - 1);
									msRdpDrive.RedirectionState = base.LocalResourceSettings.RedirectDrivesList.Value.Contains(item);
								}
							}
							else
							{
								advancedSettings.RedirectDrives = base.LocalResourceSettings.RedirectDrives.Value;
							}
							advancedSettings.RedirectPorts = base.LocalResourceSettings.RedirectPorts.Value;
							advancedSettings.RedirectPrinters = base.LocalResourceSettings.RedirectPrinters.Value;
							advancedSettings.RedirectSmartCards = base.LocalResourceSettings.RedirectSmartCards.Value;
							_client.AdvancedSettings6.RedirectClipboard = base.LocalResourceSettings.RedirectClipboard.Value;
							_client.AdvancedSettings6.RedirectDevices = base.LocalResourceSettings.RedirectPnpDevices.Value;
							str = "remote desktop attributes";
							_client.DesktopSize = GetRemoteDesktopSize();
							_client.MsRdpClient.ColorDepth = base.RemoteDesktopSettings.ColorDepth.Value;
							str = "security settings";
							_client.AdvancedSettings5.AuthenticationLevel = (uint)base.SecuritySettings.AuthenticationLevel.Value;
							if (advancedSettings2 != null)
							{
								advancedSettings2.EnableCredSspSupport = true;
								msRdpClientNonScriptable.PromptForCredentials = false;
								msRdpClientNonScriptable.NegotiateSecurityLayer = true;
							}
						}
						str = "client connection";
						_disconnectionReason = string.Empty;
						using (Helpers.Timer("invoking connect on {0} client", base.DisplayName))
						{
							_client.MsRdpClient.Connect();
						}
					}
					catch (Exception ex)
					{
						ConnectionState = RdpClient.ConnectionState.Disconnected;
						_disconnectionReason = "设置连接属性时出错";
						FormTools.ErrorDialog("错误可能涉及 '" + str + "':\n" + ex.Message);
						Log.Write("Error({1}) connecting to {0}", base.DisplayName, ex.Message);
					}
				}
			}
		}

		internal void DumpSessionState()
		{
			using (Helpers.Timer("dumping session state of {0}", base.DisplayName))
			{
				_client.Dump();
			}
		}

		internal static void SplitName(string qualifiedName, out string serverName, out int port)
		{
			string[] array = qualifiedName.Split(new char[1]
			{
				':'
			}, StringSplitOptions.RemoveEmptyEntries);
			serverName = ((array.Length > 0) ? array[0] : string.Empty);
			if (array.Length != 2 || !int.TryParse(array[1], out port))
			{
				port = -1;
			}
		}

		private void ConfigureGateway()
		{
			IMsRdpClientTransportSettings transportSettings = _client.TransportSettings;
			if (base.GatewaySettings.UseGatewayServer.Value)
			{
				uint gatewayUsageMethod = (!base.GatewaySettings.BypassGatewayForLocalAddresses.Value) ? 1u : 2u;
				transportSettings.GatewayProfileUsageMethod = 1u;
				transportSettings.GatewayUsageMethod = gatewayUsageMethod;
				uint num = transportSettings.GatewayCredsSource = (uint)base.GatewaySettings.LogonMethod.Value;
				transportSettings.GatewayHostname = base.GatewaySettings.HostName.Value;
				IMsRdpClientTransportSettings2 transportSettings2 = _client.TransportSettings2;
				if (transportSettings2 != null)
				{
					transportSettings2.GatewayCredSharing = (base.GatewaySettings.CredentialSharing.Value ? 1u : 0u);
					if (base.GatewaySettings.LogonMethod.Value == RdpClient.GatewayLogonMethod.NTLM)
					{
						transportSettings2.GatewayUsername = base.GatewaySettings.UserName.Value;
						transportSettings2.GatewayDomain = base.GatewaySettings.Domain.Value;
						transportSettings2.GatewayPassword = base.GatewaySettings.Password.Value;
					}
				}
			}
			else
			{
				transportSettings.GatewayProfileUsageMethod = 0u;
				transportSettings.GatewayUsageMethod = 0u;
			}
		}

		public override void Reconnect()
		{
			Log.Write("Begin reconnect to {0}", base.DisplayName);
			ReconnectServerRef reconnectServerRef = ReconnectGroup.Instance.AddReference(this);
			reconnectServerRef.Start(removeAfterConnection: true);
		}

		public override void Disconnect()
		{
			using (Helpers.Timer("invoking disconnect on the {0} client", base.DisplayName))
			{
				if (IsConnected)
				{
					try
					{
						_client.MsRdpClient.Disconnect();
					}
					catch (Exception ex)
					{
						Log.Write("Error disconnection: {0}", ex.Message);
					}
				}
			}
		}

		public override void LogOff()
		{
			Log.Write("Begin logoff from {0}", base.DisplayName);
			if (IsConnected)
			{
				ThreadPool.QueueUserWorkItem(LogOffWorkerProc, this);
			}
		}

		private static void LogOffWorkerProc(object o)
		{
			Server server = o as Server;
			RemoteSessions remoteSessions = new RemoteSessions(server);
			bool success = true;
			string reason = string.Empty;
			try
			{
				if (!remoteSessions.OpenServer())
				{
					success = false;
					reason = "无法访问远程会话";
				}
				else
				{
					IList<RemoteSessionInfo> list = remoteSessions.QuerySessions();
					if (list == null)
					{
						success = false;
						reason = "无法枚举远程会话";
					}
					else
					{
						int num = -1;
						foreach (RemoteSessionInfo item in list)
						{
							if (item.State == Wts.ConnectstateClass.Active && item.ClientName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) && item.UserName.Equals(server._client.MsRdpClient.UserName, StringComparison.OrdinalIgnoreCase) && item.DomainName.Equals(server._client.MsRdpClient.Domain, StringComparison.OrdinalIgnoreCase))
							{
								if (num != -1)
								{
									success = false;
									reason = "多个活动会话，无法确定要注销的会话";
									return;
								}
								num = item.SessionId;
							}
						}
						if (success)
						{
							success = remoteSessions.LogOffSession(num);
							reason = "注销会话API失败";
						}
					}
				}
			}
			catch
			{
				success = false;
				reason = "内部错误";
			}
			finally
			{
				remoteSessions.CloseServer();
				Program.TheForm.Invoke((MethodInvoker)delegate
				{
					server.LogOffResultCallback(success, reason);
				});
			}
		}

		private void LogOffResultCallback(bool success, string text)
		{
			Log.Write("End logoff from {0}", base.DisplayName);
			if (!success)
			{
				FormTools.ErrorDialog("无法从 " + base.DisplayName + " 中注销\r\n原因: " + text);
			}
		}

		private void OnConnecting(object sender, EventArgs e)
		{
			lock (_connectionStateLock)
			{
				Log.Write("OnConnecting {0}", base.DisplayName);
				UpdateOnConnectionStateChange(ImageConstants.ConnectingServer, RdpClient.ConnectionState.Connecting);
			}
		}

		private void OnConnected(object sender, EventArgs e)
		{
			lock (_connectionStateLock)
			{
				Log.Write("OnConnected {0}", base.DisplayName);
				Location = new Point(Location.X, Location.Y);
				UpdateOnConnectionStateChange(ImageConstants.ConnectedServer, RdpClient.ConnectionState.Connected);
				ServerBase serverBase = ServerTree.Instance.SelectedNode as ServerBase;
				if (serverBase != null && serverBase.ServerNode == this)
				{
					Focus();
				}
			}
		}

		private void OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
		{
			lock (_connectionStateLock)
			{
				Log.Write("OnDisconnected {0}: discReason={1} extendedDisconnectReason={2}", base.DisplayName, e.discReason, _client.MsRdpClient.ExtendedDisconnectReason);
				_disconnectionReason = string.Empty;
				DisconnectionReason disconnectionReason = null;
				if (_client.MsRdpClient.ExtendedDisconnectReason != 0)
				{
					disconnectionReason = ExtendedDisconnectionReasons.SingleOrDefault((DisconnectionReason r) => r.Code == (int)_client.MsRdpClient.ExtendedDisconnectReason);
					if (disconnectionReason == null)
					{
						_disconnectionReason = $"未知的延长断开连接原因 {_client.MsRdpClient.ExtendedDisconnectReason}";
					}
				}
				else if (e != null)
				{
					disconnectionReason = DisconnectionReasons.SingleOrDefault((DisconnectionReason r) => r.Code == e.discReason);
					if (disconnectionReason == null)
					{
						_disconnectionReason = $"未知的断开连接原因 {e.discReason}";
					}
				}
				if (disconnectionReason != null)
				{
					_disconnectionReason = disconnectionReason.Text;
				}
				if (_client.MsRdpClient.FullScreen)
				{
					ParentForm.LeaveFullScreenClient(this);
					_client.MsRdpClient.FullScreen = false;
				}
				if (IsClientDocked)
				{
					if (_client.Control.Visible)
					{
						_serverBox.Show();
					}
					DestroyClient();
				}
				UpdateOnConnectionStateChange(ImageConstants.DisconnectedServer, RdpClient.ConnectionState.Disconnected);
			}
		}

		private void OnAutoReconnecting(object sender, IMsTscAxEvents_OnAutoReconnectingEvent e)
		{
			Log.Write("OnAutoReconnecting {0}: disconnectReason={1} attemptCount={2}", base.DisplayName, e.disconnectReason, e.attemptCount);
		}

		private void OnAutoReconnecting2(object sender, IMsTscAxEvents_OnAutoReconnecting2Event e)
		{
			Log.Write("OnAutoReconnecting2 {0}: disconnectReason={1} networkAvailable={2} attemptCount={3} maxAttemptCount={4}", base.DisplayName, e.disconnectReason, e.networkAvailable, e.attemptCount, e.maxAttemptCount);
		}

		private void OnAutoReconnected(object sender, EventArgs e)
		{
			Log.Write("OnAutoReconnected {0}", base.DisplayName);
		}

		private void UpdateOnConnectionStateChange(ImageConstants image, RdpClient.ConnectionState state)
		{
			using (Helpers.Timer("changing connection state of {0} to {1}", base.DisplayName, state))
			{
				ChangeImageIndex(image);
				ConnectionState = state;
				if (_serverBox != null)
				{
					_serverBox.SetText();
				}
			}
		}

		private void OnFocusReleased(object sender, IMsTscAxEvents_OnFocusReleasedEvent e)
		{
			Log.Write("OnFocusReleased {0}: direction={1}", base.DisplayName, e.iDirection);
			NodeHelper.SelectNewActiveConnection(e.iDirection == -1);
		}

		private void OnConfirmClose(object sender, IMsTscAxEvents_OnConfirmCloseEvent e)
		{
			e.pfAllowClose = true;
		}

		private void OnRequestContainerMinimize(object sender, EventArgs e)
		{
			Log.Write("OnRequestContainerMinimize {0}", base.DisplayName);
			ParentForm.WindowState = FormWindowState.Minimized;
		}

		private void OnRequestGoFullScreen(object sender, EventArgs e)
		{
			Log.Write("OnRequestGoFullScreen {0}", base.DisplayName);
			if (_noFullScreenBehavior <= 0)
			{
				ParentForm.GoFullScreenClient(this, Program.Preferences.FullScreenWindowIsTopMost);
			}
		}

		public void SetClientSizeProperties()
		{
			if (IsClientFullScreen)
			{
				Client.AdvancedSettings2.SmartSizing = false;
				return;
			}
			InheritSettings();
			if (IsClientInPanel)
			{
				Client.AdvancedSettings2.SmartSizing = (base.IsThumbnail | DisplaySettings.SmartSizeDockedWindow.Value);
			}
			else if (IsClientInitialized)
			{
				Client.AdvancedSettings2.SmartSizing = DisplaySettings.SmartSizeUndockedWindow.Value;
			}
		}

		private void OnRequestLeaveFullScreen(object sender, EventArgs e)
		{
			Log.Write("OnRequestLeaveFullScreen {0}", base.DisplayName);
			if (_noFullScreenBehavior <= 0)
			{
				ParentForm.LeaveFullScreenClient(this);
				if (!base.IsThumbnail)
				{
					SetNormalView();
				}
			}
		}

		private void OnFatalError(object sender, IMsTscAxEvents_OnFatalErrorEvent e)
		{
			Log.Write("OnFatalError {0}: errorCode={1}", base.DisplayName, e.errorCode);
		}

		public void AddServerRef(ServerRef serverRef)
		{
			_serverRefList.Add(serverRef);
		}

		public TServerRef FindServerRef<TServerRef>() where TServerRef : ServerRef
		{
			return _serverRefList.FirstOrDefault((ServerRef r) => r is TServerRef) as TServerRef;
		}

		public TServerRef FindServerRef<TServerRef>(GroupBase parent) where TServerRef : ServerRef
		{
			return _serverRefList.FirstOrDefault((ServerRef r) => r is TServerRef && r.Parent == parent) as TServerRef;
		}

		public void RemoveServerRef(ServerRef serverRef)
		{
			_serverRefList.Remove(serverRef);
		}

		public void VisitServerRefs(Action<ServerRef> action)
		{
			ServerRef[] array = new ServerRef[_serverRefList.Count];
			_serverRefList.CopyTo(array);
			array.ForEach(action);
		}

		public override void ChangeImageIndex(ImageConstants index)
		{
			base.ChangeImageIndex(index);
			VisitServerRefs(delegate(ServerRef r)
			{
				r.ChangeImageIndex(index);
			});
		}

		public void SendRemoteAction(RemoteSessionActionType action)
		{
			IMsRdpClient8 msRdpClient = _client.MsRdpClient8;
			msRdpClient.SendRemoteAction(action);
		}

		internal override void UpdateSettings(NodePropertiesDialog nodeDialog)
		{
			base.UpdateSettings(nodeDialog);
			ServerPropertiesDialog serverPropertiesDialog = nodeDialog as ServerPropertiesDialog;
			if (serverPropertiesDialog != null)
			{
				base.Text = base.DisplayName;
				if (base.TreeView != null)
				{
					SetText();
					VisitServerRefs(delegate(ServerRef r)
					{
						r.Text = base.Text;
					});
				}
			}
		}

		internal void UpdateFromTemplate(Server template)
		{
			CopySettings(template, typeof(ServerSettings));
		}

		public override void DoPropertiesDialog(Form parentForm, string activeTabName)
		{
			using (ServerPropertiesDialog serverPropertiesDialog = ServerPropertiesDialog.NewPropertiesDialog(this, parentForm))
			{
				serverPropertiesDialog.SetActiveTab(activeTabName);
				if (serverPropertiesDialog.ShowDialog() == DialogResult.OK)
				{
					UpdateSettings(serverPropertiesDialog);
					ServerTree.Instance.OnNodeChanged(this, ChangeType.PropertyChanged);
					ServerTree.Instance.OnGroupChanged(base.Parent as GroupBase, ChangeType.InvalidateUI);
				}
			}
		}

		public override void CollectNodesToInvalidate(bool recurseChildren, HashSet<RdcTreeNode> set)
		{
			set.Add(this);
			_serverRefList.ForEach(delegate(ServerRef r)
			{
				r.CollectNodesToInvalidate(recurseChildren, set);
			});
		}

		internal override void WriteXml(XmlTextWriter tw)
		{
			tw.WriteStartElement("server");
			WriteXmlSettingsGroups(tw);
			tw.WriteEndElement();
		}

		public override bool CanDropOnTarget(RdcTreeNode targetNode)
		{
			GroupBase groupBase = (targetNode as GroupBase) ?? (targetNode.Parent as GroupBase);
			if (groupBase != null)
			{
				if (groupBase.CanDropServers())
				{
					if (groupBase.DropBehavior() != DragDropEffects.Copy)
					{
						return AllowEdit(popUI: false);
					}
					return true;
				}
				return false;
			}
			return false;
		}

		public override bool ConfirmRemove(bool askUser)
		{
			if (IsConnected)
			{
				FormTools.InformationDialog(base.Text + " 上有一个活动的会话。 删除服务器之前先断开连接。");
				return false;
			}
			return base.ConfirmRemove(askUser);
		}

		private Size GetRemoteDesktopSize()
		{
			if (base.RemoteDesktopSettings.DesktopSizeSameAsClientAreaSize.Value)
			{
				if (IsClientDocked)
				{
					return Program.TheForm.GetClientSize();
				}
				return ServerForm.ClientSize;
			}
			if (base.RemoteDesktopSettings.DesktopSizeFullScreen.Value)
			{
				return Screen.GetBounds(ParentForm).Size;
			}
			return base.RemoteDesktopSettings.DesktopSize.Value;
		}

		internal override void GoFullScreen()
		{
			if (IsConnected)
			{
				RdpClient client = Client;
				if (client != null)
				{
					client.Control.Enabled = true;
					client.MsRdpClient.FullScreen = true;
				}
			}
		}

		internal override void LeaveFullScreen()
		{
			if (IsConnected)
			{
				RdpClient client = Client;
				if (client != null)
				{
					client.MsRdpClient.FullScreen = false;
				}
			}
		}

		internal override void Undock()
		{
			if (IsClientDocked)
			{
				InitClient();
				Program.TheForm.RemoveFromClientPanel(_client.Control);
				bool visible = _client.Control.Visible;
				_client.Control.Enabled = true;
				ServerForm form = new ServerForm(this);
				Program.PluginAction(delegate(IPlugin p)
				{
					p.OnUndockServer(form);
				});
				Program.ShowForm(form);
				_serverBox.SetText();
				if (visible)
				{
					_serverBox.Show();
				}
			}
		}

		internal override void Dock()
		{
			if (IsClientUndocked)
			{
				ServerForm.Close();
				return;
			}
			_serverBox.SetText();
			if (!IsConnected)
			{
				DestroyClient();
				return;
			}
			Program.TheForm.AddToClientPanel(_client.Control);
			SetClientSizeProperties();
			if (_serverBox.Visible && !UseServerBox)
			{
				_client.Control.Size = _serverBox.Size;
				_client.Control.Location = _serverBox.Location;
				_serverBox.Hide();
			}
			else
			{
				_client.Control.Hide();
			}
			EnableDisableClient();
		}

		private void ClientGotFocus(object sender, EventArgs args)
		{
			Server.FocusReceived?.Invoke(this);
		}

		internal void EnableDisableClient()
		{
			if (IsClientInitialized && IsClientDocked)
			{
				GroupBase groupBase = base.Parent as GroupBase;
				groupBase.InheritSettings();
				Client.Control.Enabled = (!base.IsThumbnail || groupBase.DisplaySettings.AllowThumbnailSessionInteraction.Value);
			}
		}
	}
}

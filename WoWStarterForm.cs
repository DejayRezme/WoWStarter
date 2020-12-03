using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CryptoTool;

namespace WoWStarter
{
	public class WoWStarterForm : Form
	{
		protected WoWStarterConfig config;
		protected WoWResizer resizer;

		protected TableLayoutPanel layoutPanel;

		protected NumericUpDown boxNumeric;
		protected ComboBox layoutComboBox;
		protected NumericUpDown pipLeftNumeric;
		protected NumericUpDown pipTopNumeric;
		protected NumericUpDown pipWidthNumeric;
		protected NumericUpDown pipHeightNumeric;
		protected CheckBox borderlessCheckBox;
		protected CheckBox alwaysOnTopCheckBox;
		protected CheckBox mouseFocusTrackingCheckBox;
		protected CheckBox taskbarAutohideCheckBox;
		protected CheckBox maximizeHotkeyCheckBox;
		protected TextBox installPathTextBox;
		protected Button clipboardButton;
		protected Button launchButton;

		int cellPadding = 10;

		private Dictionary<String, int> hotkeys = new Dictionary<String, int>();

		public WoWStarterForm()
		{
			config = WoWStarterConfig.load();
			resizer = new WoWResizer(config);

			this.Text = "WoW Starter";
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			//this.ClientSize = new System.Drawing.Size(800, 450);
			try
			{   // try setting the app icon from the launched executable
				this.Icon = Icon.ExtractAssociatedIcon(AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".exe");
			}
			catch (System.Exception)
			{
				this.ShowIcon = false;
			}

			this.StartPosition = FormStartPosition.Manual;
			Point startPosition = System.Windows.Forms.Control.MousePosition;
			startPosition.Offset(-100, -100);
			this.Location = startPosition;
			this.AutoSize = true;
			//this.AutoSizeMode = AutoSizeMode.GrowOnly;
			// add handler to close app on esc
			this.KeyPreview = true;
			this.KeyDown += new KeyEventHandler(OnAnyKeyDown);

			// NumberBox to select number of boxes
			// ComboBox to select layout
			// Preview of layout type
			// Checkbox for borderless border
			// Checkbox for alwaysOnTop for client boxes
			// Select wow install and retail or classic
			// Optional: Checkbox to enable mouse focus tracking (while running, disable on exit)
			// Optional: Checkbox to set Taskbar autohide
			// Checkbox to keep running and enable hotkey to maximize window

			layoutPanel = new TableLayoutPanel();
			layoutPanel.AutoSize = true;
			//layoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			layoutPanel.Dock = DockStyle.Fill;
			layoutPanel.Padding = new Padding(30);
			// layoutPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
			this.Controls.Add(layoutPanel);

			int row = 0;

			// NumberBox to select number of boxes
			boxNumeric = new NumericUpDown();
			boxNumeric.AutoSize = true;
			boxNumeric.Value = config.boxCount;
			boxNumeric.Maximum = config.maxBoxCount;
			boxNumeric.Anchor = AnchorStyles.Left;
			boxNumeric.ValueChanged += new EventHandler(OnBoxCountChanged);
			AddTableLabelControl("Number of boxes:", 0, row++, boxNumeric);

			// Preview of layout type?

			// ComboBox to select layout
			layoutComboBox = new ComboBox();
			layoutComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			layoutComboBox.Items.AddRange(Enum.GetNames(typeof(MultiBoxLayouts)));
			layoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			layoutComboBox.SelectedItem = config.layout.ToString();
			//layoutComboBox.Padding = new Padding(40);
			layoutComboBox.SelectedIndexChanged += new EventHandler(OnLayoutChanged);
			AddTableLabelControl("Select Layout:", 0, row++, layoutComboBox);

			pipLeftNumeric = new NumericUpDown();
			InitNumeric(pipLeftNumeric, config.PIPPosition.Left);
			pipTopNumeric = new NumericUpDown();
			InitNumeric(pipTopNumeric, config.PIPPosition.Top);
			AddTableLabelControl("PIP Position:", 0, row++, pipLeftNumeric, pipTopNumeric, 2);
			pipWidthNumeric = new NumericUpDown();
			InitNumeric(pipWidthNumeric, config.PIPPosition.Width);
			pipHeightNumeric = new NumericUpDown();
			InitNumeric(pipHeightNumeric, config.PIPPosition.Height);
			AddTableLabelControl("PIP Size:", 0, row++, pipWidthNumeric, pipHeightNumeric, 2);
			updatePIPSizeVisible();

			borderlessCheckBox = new CheckBox();
			borderlessCheckBox.AutoSize = true;
			borderlessCheckBox.Checked = config.borderless;
			borderlessCheckBox.CheckedChanged += new EventHandler(OnBorderlessChanged);
			AddTableLabelControl("Borderless WoW: ", 0, row++, borderlessCheckBox);

			alwaysOnTopCheckBox = new CheckBox();
			alwaysOnTopCheckBox.AutoSize = true;
			alwaysOnTopCheckBox.Checked = config.alwaysOnTop;
			alwaysOnTopCheckBox.CheckedChanged += new EventHandler(OnAlwaysOnTopChanged);
			AddTableLabelControl("AlwaysOnTop clients: ", 0, row++, alwaysOnTopCheckBox);

			mouseFocusTrackingCheckBox = new CheckBox();
			mouseFocusTrackingCheckBox.AutoSize = true;
			mouseFocusTrackingCheckBox.Checked = config.mouseFocusTracking;
			mouseFocusTrackingCheckBox.CheckedChanged += new EventHandler(OnMouseFocusTrackingChanged);
			AddTableLabelControl("Enable focus tracking: ", 0, row++, mouseFocusTrackingCheckBox);

			taskbarAutohideCheckBox = new CheckBox();
			taskbarAutohideCheckBox.AutoSize = true;
			taskbarAutohideCheckBox.Checked = config.taskbarAutohide;
			taskbarAutohideCheckBox.CheckedChanged += new EventHandler(OnTaskbarAutohideChanged);
			AddTableLabelControl("Autohide taskbar: ", 0, row++, taskbarAutohideCheckBox);

			// Checkbox to keep running and enable hotkey to maximize window
			maximizeHotkeyCheckBox = new CheckBox();
			maximizeHotkeyCheckBox.AutoSize = true;
			maximizeHotkeyCheckBox.Checked = config.maximizeHotkey;
			maximizeHotkeyCheckBox.CheckedChanged += new EventHandler(OnMaximizeHotkeyChanged);
			AddTableLabelControl("Maximize hotkey: ", 0, row++, maximizeHotkeyCheckBox);
			updateHotkeyRegistry(config.maximizeHotkey);

			// type in path to wow install. Fun!
			installPathTextBox = new TextBox();
			//installPathTextBox.Text = config.installPath;
			installPathTextBox.Text = String.Join(", ", config.installPaths);
			installPathTextBox.AutoSize = true;
			installPathTextBox.ReadOnly = true;
			installPathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			checkInstallPathSet();

			Button installFileSelectorButton = new Button();
			installFileSelectorButton.Text = "Browse";
			installFileSelectorButton.AutoSize = true;
			installFileSelectorButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			installFileSelectorButton.Click += new EventHandler(OnInstallFileSelectorClicked);

			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

			AddTableLabelControl("Install path wow.exe:", 0, row++, installPathTextBox, installFileSelectorButton, 3);

			// nothing to see here, move along
			if (config.unsecureClipboardString != null) 
			{
				clipboardButton = new Button();
				clipboardButton.Text = "Copy";
				clipboardButton.AutoSize = true;
				clipboardButton.Dock = DockStyle.Right;
				clipboardButton.Click += new EventHandler(OnClipboardButtonClicked);
				layoutPanel.Controls.Add(clipboardButton);
				layoutPanel.SetCellPosition(clipboardButton, new TableLayoutPanelCellPosition(4, row++));
			}

			// Button to launch wow
			launchButton = new Button();
			launchButton.Dock = DockStyle.Top;
			launchButton.Text = "Launch WoW / Apply changes";
			launchButton.AutoSize = true;
			launchButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			launchButton.Click += new EventHandler(OnLaunchButtonClicked);
			//launchButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			layoutPanel.Controls.Add(launchButton);
			layoutPanel.SetCellPosition(launchButton, new TableLayoutPanelCellPosition(0, row));
			layoutPanel.SetColumnSpan(launchButton, 5);

			// set the minimum size for the form
			this.MinimumSize = new Size(layoutPanel.PreferredSize.Width, layoutPanel.PreferredSize.Height + 50);
		}

		private void OnClipboardButtonClicked(object sender, EventArgs e)
		{
			String passphrase = "asldfjasökdlhaeföascnklelökshv";
			if (!config.unsecureClipboardString.StartsWith("crypto")) 
				config.unsecureClipboardString = "crypto" + Crypto.Encrypt(Crypto.GetMACAddress() + config.unsecureClipboardString, passphrase);
			String clipboardString = Crypto.Decrypt(config.unsecureClipboardString.Substring(6), passphrase).Substring(12);
			System.Windows.Forms.Clipboard.SetText(clipboardString);
		}

		private void OnInstallFileSelectorClicked(object sender, EventArgs e)
		{
			FolderBrowserDialog fileDialog = new FolderBrowserDialog();
			if (Directory.Exists(config.installPaths[0]))
				fileDialog.SelectedPath = config.installPaths[0];
			else
				fileDialog.SelectedPath = Directory.GetCurrentDirectory();
			DialogResult fileResult = fileDialog.ShowDialog();

			if (fileResult == DialogResult.OK)
			{
				config.installPaths = new String[] { fileDialog.SelectedPath };
				installPathTextBox.Text = fileDialog.SelectedPath;
				checkInstallPathSet();
			}
		}

		private bool checkInstallPathSet()
		{
			bool validPath = true;
			foreach (String path in config.installPaths)
				if (!File.Exists(path + "\\wow.exe") && !File.Exists(path + "\\wowClassic.exe"))
					validPath = false;
			installPathTextBox.BackColor = DefaultBackColor;
			installPathTextBox.ForeColor = validPath ? Color.Black : Color.Red;
			return validPath;
		}

		private void OnMaximizeHotkeyChanged(object sender, EventArgs e)
		{
			config.maximizeHotkey = maximizeHotkeyCheckBox.Checked;
			updateHotkeyRegistry(config.maximizeHotkey);
		}

		private void updateHotkeyRegistry(bool enableHotkey)
		{
			registerHotkey(enableHotkey, config.maximizeHotkeyString, 1);
		}

		private void registerHotkey(bool enableHotkey, String hotkeyString, int idCode)
		{
			// try to read the hotkey definition of the config file
			int keyModifier = 0;
			int key = -1;
			foreach (String hotkeyPart in hotkeyString.Split("-"))
			{
				try
				{
					key = Enum.Parse<Keys>(hotkeyPart).GetHashCode();
				}
				catch (Exception) { }
				try
				{
					keyModifier |= (int)Enum.Parse<KeyModifier>(hotkeyPart);
				}
				catch (Exception) { }
			}

			if (key == -1)
			{
				if (enableHotkey)
					MessageBox.Show("Infalid hotkey specified in config file");
				return;
			}
			else
			{
				bool isHotkeyRegistered = hotkeys.ContainsKey(hotkeyString);
				if (enableHotkey && !isHotkeyRegistered)
				{   // hotkey should be active but hasn't been registered
					Win32Util.RegisterHotKey(this.Handle, idCode, keyModifier, key);
					hotkeys.Add(hotkeyString, idCode);
				}
				else if (!enableHotkey && isHotkeyRegistered)
				{   // hotkey shouldn't be active but still is registered
					Win32Util.UnregisterHotKey(this.Handle, idCode);
					hotkeys.Remove(hotkeyString);
				}
			}
		}

		protected void AddTableLabelControl(String text, int column, int row, Control control, Control control2 = null, int columnSpan = 4)
		{
			Label label = new Label();
			label.Text = text;
			label.AutoSize = true;
			label.Anchor = AnchorStyles.Right; // | AnchorStyles.Bottom;
			label.Padding = new Padding(cellPadding);
			layoutPanel.Controls.Add(label);
			layoutPanel.SetCellPosition(label, new TableLayoutPanelCellPosition(column, row));

			//control.Anchor = AnchorStyles.Left;
			control.Padding = new Padding(cellPadding);
			layoutPanel.Controls.Add(control);
			layoutPanel.SetCellPosition(control, new TableLayoutPanelCellPosition(column + 1, row));
			layoutPanel.SetColumnSpan(control, columnSpan);
			if (control2 != null)
			{
				//control2.Padding = new Padding(cellPadding);
				layoutPanel.Controls.Add(control2);
				layoutPanel.SetCellPosition(control2, new TableLayoutPanelCellPosition(column + 2, row));
				layoutPanel.SetColumnSpan(control2, 4 - columnSpan);
			}
		}

		private void InitNumeric(NumericUpDown pipSizeNumeric, int value)
		{
			pipSizeNumeric.AutoSize = true;
			pipSizeNumeric.Maximum = 10000;
			pipSizeNumeric.Value = value;
			pipSizeNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			pipSizeNumeric.ValueChanged += new EventHandler(OnPIPSizeChanged);
			pipSizeNumeric.Padding = new Padding(cellPadding);
			//layoutPanel.Controls.Add(pipSizeNumeric);
			//layoutPanel.SetCellPosition(pipSizeNumeric, new TableLayoutPanelCellPosition(column, row));
		}

		private void OnBoxCountChanged(object sender, EventArgs e)
		{
			config.boxCount = (int)boxNumeric.Value;
		}

		private void OnPIPSizeChanged(object sender, EventArgs e)
		{
			config.PIPPosition.Left = (int)pipLeftNumeric.Value;
			config.PIPPosition.Top = (int)pipTopNumeric.Value;
			config.PIPPosition.Width = (int)pipWidthNumeric.Value;
			config.PIPPosition.Height = (int)pipHeightNumeric.Value;
			//updateWoWClientsIfRunning();
		}

		private void updatePIPSizeVisible()
		{
			pipLeftNumeric.Enabled = config.layout == MultiBoxLayouts.PIPVertical;
			pipTopNumeric.Enabled = config.layout == MultiBoxLayouts.PIPVertical;
			pipWidthNumeric.Enabled = config.layout == MultiBoxLayouts.PIPVertical;
			pipHeightNumeric.Enabled = config.layout == MultiBoxLayouts.PIPVertical;
			if (config.layout == MultiBoxLayouts.CustomConfig)
				boxNumeric.Value = config.customLayout.Length;
			boxNumeric.Enabled = config.layout != MultiBoxLayouts.CustomConfig;
		}

		private void updateWoWClientsIfRunning()
		{
			if (resizer.isLaunched())
			{
				resizer.LaunchWoWClients();
				this.BringToFront();
			}
		}

		private void OnLayoutChanged(object sender, EventArgs e)
		{
			String layoutName = layoutComboBox.SelectedItem.ToString();
			config.layout = (MultiBoxLayouts)Enum.Parse(typeof(MultiBoxLayouts), layoutName);
			updatePIPSizeVisible();
			//updateWoWClientsIfRunning();
		}

		private void OnTaskbarAutohideChanged(object sender, EventArgs e)
		{
			config.taskbarAutohide = taskbarAutohideCheckBox.Checked;
			Win32Util.setTaskbarAutohide(config.taskbarAutohide);
		}

		private void OnMouseFocusTrackingChanged(object sender, EventArgs e)
		{
			config.mouseFocusTracking = mouseFocusTrackingCheckBox.Checked;
			Win32Util.setMouseFocusTracking(config.mouseFocusTracking);
		}

		private void OnAlwaysOnTopChanged(object sender, EventArgs e)
		{
			config.alwaysOnTop = alwaysOnTopCheckBox.Checked;
			//updateWoWClientsIfRunning();
		}

		private void OnBorderlessChanged(object sender, EventArgs e)
		{
			config.borderless = borderlessCheckBox.Checked;
			//updateWoWClientsIfRunning();
		}

		protected void OnLaunchButtonClicked(object sender, EventArgs e)
		{
			if (!checkInstallPathSet())
			{
				MessageBox.Show("Wow.exe not found! Please select an retail directory containing wow.exe or the classic directory containing wowClassic.exe!", "Wow.exe not found");
			}
			else
			{
				resizer.LaunchWoWClients();
				this.BringToFront();
			}
		}

		protected void OnAnyKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Escape)
				this.Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			config.save();
			if (config.closeWoWWithApp)
				resizer.CloseWoWClients();
			updateHotkeyRegistry(false);
			base.OnClosing(e);
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (m.Msg == 0x0312)
			{
				Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
				KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);
				int id = m.WParam.ToInt32();

				resizer.maximizeHotkey(id == 1);
				//MessageBox.Show("Hotkey has been pressed!" + id + " Key: " + key + " Modifier: " + modifier);
			}
		}
	}
}

/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using BulkCrapUninstaller.Functions;
using BulkCrapUninstaller.Properties;
using Klocman;
using Klocman.Binding.Settings;
using Klocman.Forms.Theming;
using Klocman.Forms.Tools;
using Klocman.Localising;

namespace BulkCrapUninstaller.Forms
{
    public partial class SettingsWindow : Form
    {
        private readonly SettingBinder<Settings> _settings = Settings.Default.SettingBinder;
        private bool _restartNeeded;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int OpenedTab { get { return tabControl.SelectedIndex; } set { tabControl.SelectedIndex = value; } }

        public SettingsWindow()
        {
            InitializeComponent();
            Klocman.Forms.Theming.ThemeManager.Attach(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode) return;

            Icon = Resources.Icon_Logo;

            _settings.BindControl(checkBoxLoud, x => x.MessagesAskRemoveLoudItems, this);
            _settings.BindControl(checkBoxShowAllBadJunk, x => x.MessagesShowAllBadJunk, this);
            _settings.BindControl(checkBoxNeverFeedback, x => x.MiscFeedbackNagNeverShow, this);
            _settings.BindControl(checkBoxUpdateSearch, x => x.MiscCheckForUpdates, this);
            _settings.BindControl(checkBoxSendStats, x => x.MiscSendStatistics, this);
            _settings.BindControl(checkBoxAutoLoad, x => x.MiscAutoLoadDefaultList, this);
            _settings.BindControl(checkBoxRatings, x => x.MiscUserRatings, this);
            _settings.BindControl(checkBoxColorblind, x => x.MiscColorblind, this);
            _settings.BindControl(checkBoxDpiaware, x => x.WindowDpiAware, this);

            _settings.BindControl(checkBoxEnableExternal, x => x.ExternalEnable, this);
            _settings.BindControl(textBoxPreUninstall, x => x.ExternalPreCommands, this);
            _settings.BindControl(textBoxPostUninstall, x => x.ExternalPostCommands, this);

            _settings.BindControl(textBoxProgramFolders, x => x.FoldersCustomProgramDirs, this);
            _settings.BindControl(checkBoxAutoInstallFolderDetect, x => x.FoldersAutoDetect, this);
            _settings.BindControl(checkBoxRemovable, x => x.FoldersScanRemovable, this);
            _settings.Subscribe((x, y) => checkBoxRemovable.Enabled = y.NewValue, x => x.FoldersAutoDetect, this);

            _settings.BindControl(checkBoxChoco, x => x.ScanChocolatey, this);
            _settings.BindControl(checkBoxScoop, x => x.ScanScoop, this);
            _settings.BindControl(checkBoxScanSteam, x => x.ScanSteam, this);
            _settings.BindControl(checkBoxScanStoreApps, x => x.ScanStoreApps, this);
            _settings.BindControl(checkBoxOculus, x => x.ScanOculus, this);
            _settings.BindControl(checkBoxScanWinFeatures, x => x.ScanWinFeatures, this);
            _settings.BindControl(checkBoxScanWinUpdates, x => x.ScanWinUpdates, this);

            _settings.BindControl(checkBoxScanDrives, x => x.ScanDrives, this);
            _settings.BindControl(checkBoxScanRegistry, x => x.ScanRegistry, this);
            _settings.BindControl(checkBoxPreDefined, x => x.ScanPreDefined, this);

            foreach (YesNoAsk value in Enum.GetValues(typeof(YesNoAsk)))
            {
                var wrapper = new LocalisedEnumWrapper(value);
                comboBoxJunk.Items.Add(wrapper);
                comboBoxRestore.Items.Add(wrapper);
            }
            _settings.Subscribe(JunkSettingChanged, x => x.MessagesRemoveJunk, this);
            _settings.Subscribe(RestoreSettingChanged, x => x.MessagesRestorePoints, this);

            foreach (UninstallerListDoubleClickAction value in Enum.GetValues(typeof(UninstallerListDoubleClickAction)))
            {
                var wrapper = new LocalisedEnumWrapper(value);
                comboBoxDoubleClick.Items.Add(wrapper);
            }
            _settings.Subscribe(DoubleClickSettingChanged, x => x.UninstallerListDoubleClickAction, this);

            comboBoxLanguage.Items.Add(Localisable.DefaultLanguage);
            foreach (var languageCode in CultureConfigurator.SupportedLanguages.OrderBy(x => x.DisplayName))
            {
                comboBoxLanguage.Items.Add(new ComboBoxWrapper<CultureInfo>(languageCode, x => x.DisplayName));
            }
            _settings.Subscribe(LanguageSettingChanged, x => x.Language, this);

            _settings.Subscribe(BackupSettingChanged, x => x.BackupLeftovers, this);
            _settings.BindProperty(directorySelectBoxBackup,
                box => box.DirectoryPath, nameof(directorySelectBoxBackup.DirectoryPathChanged),
                settings => settings.BackupLeftoversDirectory, this);

            _settings.SendUpdates(this);

            AddThemeSelector();

            _restartNeeded = false;
        }

        #region Color theme

        private ComboBox _comboBoxTheme;
        private bool _themeRestartNeeded;

        /// <summary>
        /// Index of the theme that is currently in use: 0 dark, 1 light, 2 follow Windows, 3 classic (no theme)
        /// </summary>
        private static int RunningThemeIndex => ThemeManager.IsActive ? (int)ThemeManager.Mode : 3;

        private static int SelectedThemeIndex(Settings s) => s.WindowUseSystemTheme ? 3 : (int)s.WindowTheme;

        /// <summary>
        /// Adds the color theme selector to the Misc group, laid out like the double click selector below it.
        /// </summary>
        private void AddThemeSelector()
        {
            var texts = GetThemeTexts();

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Margin = panel5.Margin,
                Padding = panel5.Padding,
                Name = "panelTheme"
            };
            _comboBoxTheme = new ComboBox
            {
                Dock = DockStyle.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Width = comboBoxDoubleClick.Width,
                Name = "comboBoxTheme"
            };
            _comboBoxTheme.Items.AddRange(new object[] { texts[1], texts[2], texts[3], texts[4] });
            var label = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Left,
                Margin = label3.Margin,
                Padding = label3.Padding,
                Text = texts[0],
                Name = "labelTheme"
            };
            panel.Controls.Add(_comboBoxTheme);
            panel.Controls.Add(label);
            panel.Height = panel5.Height;

            groupBoxMisc.SuspendLayout();
            groupBoxMisc.Controls.Add(panel);
            groupBoxMisc.ResumeLayout(true);

            var index = SelectedThemeIndex(_settings.Settings);
            _comboBoxTheme.SelectedIndex = Math.Max(0, Math.Min(3, index));
            _comboBoxTheme.SelectedIndexChanged += ComboBoxTheme_SelectedIndexChanged;
        }

        private void ComboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = _comboBoxTheme.SelectedIndex;
            if (index < 0) return;

            if (index == 3)
            {
                _settings.Settings.WindowUseSystemTheme = true;
            }
            else
            {
                _settings.Settings.WindowTheme = (ThemeMode)index;
                _settings.Settings.WindowUseSystemTheme = false;
            }

            // The theme is applied when windows are created, so a restart is needed to see the change
            _themeRestartNeeded = index != RunningThemeIndex;
        }

        /// <summary>
        /// Label and option names (label, dark, light, follow Windows, classic). Kept out of the resource files.
        /// </summary>
        private static string[] GetThemeTexts()
        {
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "it":
                    return new[] { "Tema colori:", "Scuro", "Chiaro", "Segui Windows", "Classico (senza tema)" };
                case "de":
                    return new[] { "Farbschema:", "Dunkel", "Hell", "Wie Windows", "Klassisch (ohne Design)" };
                case "fr":
                    return new[] { "Thème de couleurs :", "Sombre", "Clair", "Suivre Windows", "Classique (sans thème)" };
                case "es":
                    return new[] { "Tema de color:", "Oscuro", "Claro", "Seguir a Windows", "Clásico (sin tema)" };
                default:
                    return new[] { "Color theme:", "Dark", "Light", "Follow Windows", "Classic (no theme)" };
            }
        }

        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            Close();

            if ((_restartNeeded || _themeRestartNeeded) && MessageBoxes.RestartNeededForSettingChangeQuestion())
            {
                EntryPoint.Restart();
            }
        }

        private void checkBoxEnableExternal_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer1.Enabled = checkBoxEnableExternal.Checked;
            //textBoxPreUninstall.Enabled = checkBoxEnableExternal.Checked;
            //textBoxPostUninstall.Enabled = checkBoxEnableExternal.Checked;
        }

        private void comboBoxJunk_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxJunk.SelectedItem is LocalisedEnumWrapper wrapper)
            {
                _settings.Settings.MessagesRemoveJunk = (YesNoAsk)wrapper.TargetEnum;
            }
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxLanguage.SelectedItem is ComboBoxWrapper<CultureInfo> wrapper)
            {
                _settings.Settings.Language = wrapper.WrappedObject.Name;
                _restartNeeded = true;
            }
            else if (comboBoxLanguage.SelectedItem is string)
            {
                _settings.Settings.Language = string.Empty;
                _restartNeeded = true;
            }
        }

        private void comboBoxRestore_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxRestore.SelectedItem is LocalisedEnumWrapper wrapper)
            {
                _settings.Settings.MessagesRestorePoints = (YesNoAsk)wrapper.TargetEnum;
            }
        }

        private void JunkSettingChanged(object sender, SettingChangedEventArgs<YesNoAsk> args)
        {
            var newSelection =
                comboBoxJunk.Items.Cast<LocalisedEnumWrapper>().FirstOrDefault(x => x.TargetEnum.Equals(args.NewValue));
            if (newSelection == null || newSelection.Equals(comboBoxJunk.SelectedItem))
                return;

            comboBoxJunk.SelectedItem = newSelection;
        }

        private void LanguageSettingChanged(object sender, SettingChangedEventArgs<string> args)
        {
            if (!string.IsNullOrEmpty(args.NewValue))
            {
                var selectedItem = comboBoxLanguage.Items.OfType<ComboBoxWrapper<CultureInfo>>()
                    .FirstOrDefault(x => x.WrappedObject.Name.Equals(args.NewValue));
                if (selectedItem != null)
                {
                    comboBoxLanguage.SelectedItem = selectedItem;
                    return;
                }
            }
            comboBoxLanguage.SelectedIndex = 0;
        }

        private void RestoreSettingChanged(object sender, SettingChangedEventArgs<YesNoAsk> args)
        {
            var newSelection =
                comboBoxRestore.Items.Cast<LocalisedEnumWrapper>()
                    .FirstOrDefault(x => x.TargetEnum.Equals(args.NewValue));
            if (newSelection == null || newSelection.Equals(comboBoxRestore.SelectedItem))
                return;

            comboBoxRestore.SelectedItem = newSelection;
        }

        private void SettingsWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            _settings.RemoveHandlers(this);
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void radioButtonBackup_CheckedChanged(object sender, EventArgs e)
        {
            directorySelectBoxBackup.Enabled = false;

            if (radioButtonBackupAsk.Checked)
                _settings.Settings.BackupLeftovers = YesNoAsk.Ask;
            else if (radioButtonBackupAuto.Checked)
            {
                _settings.Settings.BackupLeftovers = YesNoAsk.Yes;
                directorySelectBoxBackup.Enabled = true;
            }
            else if (radioButtonBackupNever.Checked)
                _settings.Settings.BackupLeftovers = YesNoAsk.No;
            else
                throw new InvalidOperationException();
        }

        private void BackupSettingChanged(object sender, SettingChangedEventArgs<YesNoAsk> args)
        {
            switch (args.NewValue)
            {
                case YesNoAsk.Ask:
                    radioButtonBackupAsk.Checked = true;
                    break;
                case YesNoAsk.Yes:
                    radioButtonBackupAuto.Checked = true;
                    break;
                case YesNoAsk.No:
                    radioButtonBackupNever.Checked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args), args.NewValue, "Unknown value");
            }
        }

        private void comboBoxDoubleClick_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxDoubleClick.SelectedItem is LocalisedEnumWrapper wrapper)
            {
                _settings.Settings.UninstallerListDoubleClickAction = (UninstallerListDoubleClickAction)wrapper.TargetEnum;
            }
        }

        private void DoubleClickSettingChanged(object sender, SettingChangedEventArgs<UninstallerListDoubleClickAction> args)
        {
            var newSelection = comboBoxDoubleClick.Items.Cast<LocalisedEnumWrapper>().FirstOrDefault(x => x.TargetEnum.Equals(args.NewValue));
            if (newSelection == null || newSelection.Equals(comboBoxDoubleClick.SelectedItem))
                return;

            comboBoxDoubleClick.SelectedItem = newSelection;
        }
    }
}
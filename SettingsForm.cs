using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatGPT
{
    /// <summary>
    /// Отдельное центрированное окно настроек.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly UserSettings _settings;
        private readonly CheckBox _cbRememberLast;
        private readonly CheckBox _cbRememberSize;
        private readonly CheckBox _cbBypassOnStartup;

        public SettingsForm(UserSettings settings, bool isAdmin)
        {
            _settings = settings;

            Text = "Настройки";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(320, 190);
            Font = new Font("Segoe UI", 9.5f);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 4,
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _cbRememberLast = new CheckBox
            {
                Text = "Запоминать последний",
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4),
                Checked = _settings.RememberLast,
            };
            _cbRememberSize = new CheckBox
            {
                Text = "Запоминать размер окна",
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4),
                Checked = _settings.RememberWindowSize,
            };
            _cbBypassOnStartup = new CheckBox
            {
                Text = "Обход при запуске",
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4),
                Checked = _settings.BypassOnStartup && isAdmin,
                Enabled = isAdmin,
            };
            if (!isAdmin)
            {
                _cbBypassOnStartup.Text += " (нужны права администратора)";
            }

            layout.Controls.Add(_cbRememberLast, 0, 0);
            layout.Controls.Add(_cbRememberSize, 0, 1);
            layout.Controls.Add(_cbBypassOnStartup, 0, 2);

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0),
            };
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 84 };
            var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 84 };
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);
            layout.Controls.Add(buttons, 0, 3);

            Controls.Add(layout);
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            btnOk.Click += (s, e) =>
            {
                _settings.RememberLast = _cbRememberLast.Checked;
                _settings.RememberWindowSize = _cbRememberSize.Checked;
                _settings.BypassOnStartup = _cbBypassOnStartup.Checked;
                _settings.Save();
            };
        }
    }
}

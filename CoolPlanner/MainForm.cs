using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CoolPlanner
{
    public class MainForm : Form
    {
        private DataStore _data;
        private BindingList<Direction> _directions;
        private BindingList<EventItem> _events;
        private BindingList<Participant> _participants;

        private TabControl tabs;
        private TabPage pageDirections;
        private TabPage pageEvents;
        private TabPage pageParticipants;
        private TabPage pageReports;

        private DataGridView gridDirections;
        private DataGridView gridEvents;
        private DataGridView gridParticipants;

        private TextBox txtDirectionName;
        private TextBox txtDirectionDescription;

        private ComboBox cmbEventDirection;
        private TextBox txtEventTitle;
        private TextBox txtEventDate;
        private TextBox txtEventTime;
        private TextBox txtEventPlace;
        private TextBox txtEventResponsible;

        private TextBox txtParticipantName;
        private TextBox txtParticipantGroup;
        private TextBox txtParticipantEmail;
        private TextBox txtParticipantPhone;

        private TextBox txtReport;
        private ToolStripStatusLabel lblStatus;

        // --- доработка по практической работе №2 ---
        private Timer reminderTimer;
        private CheckBox chkReminders;
        private readonly HashSet<int> _notified = new HashSet<int>();

        public MainForm()
        {
            _data = Storage.Load();

            _directions = new BindingList<Direction>(_data.Directions);
            _events = new BindingList<EventItem>(_data.Events);
            _participants = new BindingList<Participant>(_data.Participants);

            BuildInterface();
            RefreshDirectionCombo();
            UpdateStatus();

            // Фоновый таймер напоминаний: проверка расписания раз в минуту.
            reminderTimer = new Timer { Interval = 60000 };
            reminderTimer.Tick += OnReminderTick;
            reminderTimer.Start();
        }

        // ------------------------------------------------------------------ UI

        private void BuildInterface()
        {
            SuspendLayout();

            Text = "CoolPlanner - планирование мероприятий";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1060, 660);
            MinimumSize = new Size(900, 560);
            Font = new Font("Segoe UI", 9F);

            var menu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            fileMenu.DropDownItems.Add("Сохранить", null, OnSaveClick);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Выход", null, OnExitClick);

            var helpMenu = new ToolStripMenuItem("Справка");
            helpMenu.DropDownItems.Add("О программе", null, OnAboutClick);

            menu.Items.Add(fileMenu);
            menu.Items.Add(helpMenu);

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;

            pageDirections = new TabPage("Направления");
            pageEvents = new TabPage("Мероприятия");
            pageParticipants = new TabPage("Участники");
            pageReports = new TabPage("Отчёты");

            BuildDirectionsPage();
            BuildEventsPage();
            BuildParticipantsPage();
            BuildReportsPage();

            tabs.TabPages.Add(pageDirections);
            tabs.TabPages.Add(pageEvents);
            tabs.TabPages.Add(pageParticipants);
            tabs.TabPages.Add(pageReports);

            var status = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("");
            status.Items.Add(lblStatus);

            MainMenuStrip = menu;
            Controls.Add(tabs);
            Controls.Add(status);
            Controls.Add(menu);

            ResumeLayout(false);
            PerformLayout();
        }

        private void BuildDirectionsPage()
        {
            gridDirections = CreateGrid();
            AddColumn(gridDirections, "Название", "Name", 40);
            AddColumn(gridDirections, "Описание", "Description", 60);
            gridDirections.DataSource = _directions;

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 110 };

            panel.Controls.Add(MakeLabel("Название:", 12, 16));
            txtDirectionName = MakeText(95, 12, 260);

            panel.Controls.Add(MakeLabel("Описание:", 12, 52));
            txtDirectionDescription = MakeText(95, 48, 620);

            panel.Controls.Add(MakeLabel("Например: «Конференции», «Концерты», «Спорт»", 380, 16));

            var add = MakeButton("Добавить направление", 730, 12, 190);
            add.Click += OnAddDirection;

            var remove = MakeButton("Удалить выбранное", 730, 48, 190);
            remove.Click += OnRemoveDirection;

            panel.Controls.AddRange(new Control[]
            {
                txtDirectionName, txtDirectionDescription, add, remove
            });

            pageDirections.Controls.Add(gridDirections);
            pageDirections.Controls.Add(panel);
        }

        private void BuildEventsPage()
        {
            gridEvents = CreateGrid();
            AddColumn(gridEvents, "Направление", "DirectionName", 20);
            AddColumn(gridEvents, "Мероприятие", "Title", 28);
            AddColumn(gridEvents, "Дата", "Date", 12);
            AddColumn(gridEvents, "Время", "Time", 10);
            AddColumn(gridEvents, "Место", "Place", 15);
            AddColumn(gridEvents, "Ответственный", "Responsible", 15);
            gridEvents.DataSource = _events;
            gridEvents.DataBindingComplete += delegate { FillDirectionNames(); };

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 150 };

            panel.Controls.Add(MakeLabel("Направление:", 12, 16));
            cmbEventDirection = new ComboBox
            {
                Location = new Point(105, 12),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            panel.Controls.Add(MakeLabel("Название:", 325, 16));
            txtEventTitle = MakeText(400, 12, 180);

            panel.Controls.Add(MakeLabel("Дата:", 600, 16));
            txtEventDate = MakeText(645, 12, 90);

            panel.Controls.Add(MakeLabel("Время:", 755, 16));
            txtEventTime = MakeText(810, 12, 60);

            panel.Controls.Add(MakeLabel("Место:", 12, 52));
            txtEventPlace = MakeText(105, 48, 200);

            panel.Controls.Add(MakeLabel("Ответственный:", 325, 52));
            txtEventResponsible = MakeText(430, 48, 200);

            var add = MakeButton("Добавить мероприятие", 12, 92, 200);
            add.Click += OnAddEvent;

            var remove = MakeButton("Удалить выбранное", 224, 92, 190);
            remove.Click += OnRemoveEvent;

            // Доработка: выгрузка отчёта в CSV и управление напоминаниями.
            var export = MakeButton("Выгрузить отчёт (CSV)", 426, 92, 200);
            export.Click += OnExportCsv;

            chkReminders = new CheckBox
            {
                Text = "Напоминать о мероприятиях",
                Location = new Point(646, 96),
                AutoSize = true,
                Checked = true
            };
            chkReminders.CheckedChanged += OnRemindersToggled;

            panel.Controls.AddRange(new Control[]
            {
                cmbEventDirection, txtEventTitle, txtEventDate, txtEventTime,
                txtEventPlace, txtEventResponsible, add, remove, export, chkReminders
            });

            pageEvents.Controls.Add(gridEvents);
            pageEvents.Controls.Add(panel);
        }

        private void BuildParticipantsPage()
        {
            gridParticipants = CreateGrid();
            AddColumn(gridParticipants, "ФИО", "FullName", 34);
            AddColumn(gridParticipants, "Группа", "Group", 16);
            AddColumn(gridParticipants, "E-mail", "Email", 28);
            AddColumn(gridParticipants, "Телефон", "Phone", 22);
            gridParticipants.DataSource = _participants;

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 110 };

            panel.Controls.Add(MakeLabel("ФИО:", 12, 16));
            txtParticipantName = MakeText(95, 12, 260);

            panel.Controls.Add(MakeLabel("Группа:", 375, 16));
            txtParticipantGroup = MakeText(435, 12, 100);

            panel.Controls.Add(MakeLabel("E-mail:", 555, 16));
            txtParticipantEmail = MakeText(610, 12, 180);

            panel.Controls.Add(MakeLabel("Телефон:", 12, 52));
            txtParticipantPhone = MakeText(95, 48, 160);

            var add = MakeButton("Добавить участника", 810, 12, 190);
            add.Click += OnAddParticipant;

            var remove = MakeButton("Удалить выбранное", 810, 48, 190);
            remove.Click += OnRemoveParticipant;

            panel.Controls.AddRange(new Control[]
            {
                txtParticipantName, txtParticipantGroup, txtParticipantEmail,
                txtParticipantPhone, add, remove
            });

            pageParticipants.Controls.Add(gridParticipants);
            pageParticipants.Controls.Add(panel);
        }

        private void BuildReportsPage()
        {
            txtReport = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10F),
                BackColor = Color.White
            };

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 48 };

            var build = MakeButton("Сформировать отчёт", 12, 8, 200);
            build.Click += OnBuildReport;

            var clear = MakeButton("Очистить", 224, 8, 140);
            clear.Click += delegate { txtReport.Clear(); };

            panel.Controls.Add(build);
            panel.Controls.Add(clear);

            pageReports.Controls.Add(txtReport);
            pageReports.Controls.Add(panel);
        }

        // ------------------------------------------------------------- разное

        private void RefreshDirectionCombo()
        {
            cmbEventDirection.DataSource = null;
            cmbEventDirection.DataSource = _directions;
            cmbEventDirection.DisplayMember = "Name";
            cmbEventDirection.ValueMember = "Id";
        }

        private void FillDirectionNames()
        {
            foreach (var item in _events)
            {
                var direction = _directions.FirstOrDefault(d => d.Id == item.DirectionId);
                item.DirectionName = direction == null ? "не задано" : direction.Name;
            }

            gridEvents.Refresh();
        }

        private void SaveData()
        {
            _data.Directions = _directions.ToList();
            _data.Events = _events.ToList();
            _data.Participants = _participants.ToList();
            Storage.Save(_data);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            lblStatus.Text = string.Format(
                "Направлений: {0}   Мероприятий: {1}   Участников: {2}   Файл данных: {3}",
                _directions.Count, _events.Count, _participants.Count, Storage.FileLocation);
        }

        private void Warn(string message)
        {
            MessageBox.Show(this, message, "Проверка данных",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ----------------------------------------------------------- handlers

        private void OnAddDirection(object sender, EventArgs e)
        {
            string name = txtDirectionName.Text.Trim();

            if (name.Length == 0)
            {
                Warn("Укажите название направления.");
                return;
            }

            if (_directions.Any(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                Warn("Такое направление уже есть.");
                return;
            }

            _directions.Add(new Direction
            {
                Id = _data.NextDirectionId++,
                Name = name,
                Description = txtDirectionDescription.Text.Trim()
            });

            txtDirectionName.Clear();
            txtDirectionDescription.Clear();
            SaveData();
        }

        private void OnRemoveDirection(object sender, EventArgs e)
        {
            var item = Selected<Direction>(gridDirections);

            if (item == null)
            {
                Warn("Выберите направление в таблице.");
                return;
            }

            if (_events.Any(ev => ev.DirectionId == item.Id))
            {
                Warn("Нельзя удалить направление, пока к нему привязаны мероприятия.");
                return;
            }

            _directions.Remove(item);
            SaveData();
        }

        private void OnAddEvent(object sender, EventArgs e)
        {
            var direction = cmbEventDirection.SelectedItem as Direction;

            if (direction == null)
            {
                Warn("Сначала добавьте направление.");
                return;
            }

            if (txtEventTitle.Text.Trim().Length == 0)
            {
                Warn("Укажите название мероприятия.");
                return;
            }

            DateTime parsed;
            if (!DateTime.TryParse(txtEventDate.Text.Trim(), out parsed))
            {
                Warn("Дата указывается в формате дд.мм.гггг.");
                return;
            }

            if (!DateTime.TryParse(txtEventTime.Text.Trim(), out parsed))
            {
                Warn("Время указывается в формате чч:мм.");
                return;
            }

            _events.Add(new EventItem
            {
                Id = _data.NextEventId++,
                DirectionId = direction.Id,
                DirectionName = direction.Name,
                Title = txtEventTitle.Text.Trim(),
                Date = txtEventDate.Text.Trim(),
                Time = txtEventTime.Text.Trim(),
                Place = txtEventPlace.Text.Trim(),
                Responsible = txtEventResponsible.Text.Trim()
            });

            txtEventTitle.Clear();
            txtEventDate.Clear();
            txtEventTime.Clear();
            txtEventPlace.Clear();
            txtEventResponsible.Clear();
            SaveData();
        }

        private void OnRemoveEvent(object sender, EventArgs e)
        {
            var item = Selected<EventItem>(gridEvents);

            if (item == null)
            {
                Warn("Выберите мероприятие в таблице.");
                return;
            }

            _events.Remove(item);
            SaveData();
        }

        private void OnAddParticipant(object sender, EventArgs e)
        {
            string name = txtParticipantName.Text.Trim();

            if (name.Length == 0)
            {
                Warn("Укажите ФИО участника.");
                return;
            }

            _participants.Add(new Participant
            {
                Id = _data.NextParticipantId++,
                FullName = name,
                Group = txtParticipantGroup.Text.Trim(),
                Email = txtParticipantEmail.Text.Trim(),
                Phone = txtParticipantPhone.Text.Trim()
            });

            txtParticipantName.Clear();
            txtParticipantGroup.Clear();
            txtParticipantEmail.Clear();
            txtParticipantPhone.Clear();
            SaveData();
        }

        private void OnRemoveParticipant(object sender, EventArgs e)
        {
            var item = Selected<Participant>(gridParticipants);

            if (item == null)
            {
                Warn("Выберите участника в таблице.");
                return;
            }

            _participants.Remove(item);
            SaveData();
        }

        private void OnBuildReport(object sender, EventArgs e)
        {
            var text = new StringBuilder();
            text.AppendLine("СВОДНЫЙ ОТЧЁТ ПО ПЛАНУ МЕРОПРИЯТИЙ");
            text.AppendLine("Дата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            text.AppendLine(new string('=', 78));
            text.AppendLine();

            text.AppendLine("1. Мероприятия по направлениям");
            text.AppendLine(new string('-', 78));

            foreach (var direction in _directions)
            {
                int count = _events.Count(ev => ev.DirectionId == direction.Id);
                text.AppendLine(string.Format("  {0,-40} {1,3} мер.", direction.Name, count));
            }

            if (_directions.Count == 0)
            {
                text.AppendLine("  Направления не заданы.");
            }

            text.AppendLine();
            text.AppendLine("2. Ближайшие мероприятия");
            text.AppendLine(new string('-', 78));

            var upcoming = _events
                .Select(ev => new { Event = ev, Moment = ParseMoment(ev) })
                .Where(x => x.Moment.HasValue && x.Moment.Value >= DateTime.Now.Date)
                .OrderBy(x => x.Moment.Value)
                .Take(10)
                .ToList();

            if (upcoming.Count == 0)
            {
                text.AppendLine("  Запланированных мероприятий нет.");
            }

            foreach (var item in upcoming)
            {
                text.AppendLine(string.Format("  {0:dd.MM.yyyy} {1,-5}  {2,-30} {3}",
                    item.Moment.Value,
                    item.Event.Time,
                    item.Event.Title,
                    item.Event.Place));
            }

            text.AppendLine();
            text.AppendLine("3. Участники");
            text.AppendLine(new string('-', 78));

            foreach (var participant in _participants)
            {
                text.AppendLine(string.Format("  {0,-32} {1,-12} {2}",
                    participant.FullName, participant.Group, participant.Email));
            }

            if (_participants.Count == 0)
            {
                text.AppendLine("  Участники не внесены.");
            }

            text.AppendLine();
            text.AppendLine(new string('=', 78));
            text.AppendLine(string.Format("Итого: направлений {0}, мероприятий {1}, участников {2}.",
                _directions.Count, _events.Count, _participants.Count));

            txtReport.Text = text.ToString();
        }

        // ------------------------------------------- доработка по ПР2

        /// <summary>Выгрузка списка мероприятий в CSV (разделитель — точка с запятой).</summary>
        private void OnExportCsv(object sender, EventArgs e)
        {
            if (_events.Count == 0)
            {
                Warn("Список мероприятий пуст, выгружать нечего.");
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Выгрузка отчёта о мероприятиях";
                dialog.Filter = "Файл CSV (*.csv)|*.csv";
                dialog.FileName = "Отчет_по_мероприятиям_" + DateTime.Now.ToString("yyyy-MM-dd");

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var text = new StringBuilder();
                text.AppendLine("Направление;Мероприятие;Дата;Время;Место;Ответственный");

                foreach (var item in _events)
                {
                    text.AppendLine(string.Join(";",
                        CsvValue(item.DirectionName),
                        CsvValue(item.Title),
                        CsvValue(item.Date),
                        CsvValue(item.Time),
                        CsvValue(item.Place),
                        CsvValue(item.Responsible)));
                }

                try
                {
                    File.WriteAllText(dialog.FileName, text.ToString(), new UTF8Encoding(true));

                    MessageBox.Show(this,
                        "Отчёт выгружен:\n" + dialog.FileName + "\n\nСтрок: " + _events.Count,
                        "Выгрузка отчёта",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception error)
                {
                    MessageBox.Show(this, "Не удалось сохранить файл:\n" + error.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string CsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value.IndexOf(';') >= 0 || value.IndexOf('"') >= 0 ||
                value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        /// <summary>Включение и выключение фоновых напоминаний.</summary>
        private void OnRemindersToggled(object sender, EventArgs e)
        {
            if (reminderTimer != null)
            {
                reminderTimer.Enabled = chkReminders.Checked;
            }
        }

        /// <summary>Проверка ближайших мероприятий и вывод напоминания.</summary>
        private void OnReminderTick(object sender, EventArgs e)
        {
            if (chkReminders != null && !chkReminders.Checked)
            {
                return;
            }

            var now = DateTime.Now;

            foreach (var item in _events)
            {
                var moment = ParseMoment(item);

                if (!moment.HasValue)
                {
                    continue;
                }

                var left = moment.Value - now;

                if (left.TotalMinutes < 0 || left.TotalMinutes > 30)
                {
                    continue;
                }

                if (_notified.Contains(item.Id))
                {
                    continue;
                }

                _notified.Add(item.Id);

                MessageBox.Show(this,
                    string.Format("Через {0} мин. начнётся мероприятие:\n«{1}»\n{2}, {3}\nМесто: {4}",
                        (int)Math.Ceiling(left.TotalMinutes),
                        item.Title,
                        item.Date,
                        item.Time,
                        item.Place),
                    "Напоминание о мероприятии",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private static DateTime? ParseMoment(EventItem item)
        {
            DateTime date;

            if (!DateTime.TryParse(item.Date, out date))
            {
                return null;
            }

            DateTime time;

            if (DateTime.TryParse(item.Time, out time))
            {
                return date.Date.Add(time.TimeOfDay);
            }

            return date.Date;
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            SaveData();
            MessageBox.Show(this, "Данные сохранены.", "CoolPlanner",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            Close();
        }

        private void OnAboutClick(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "CoolPlanner - учебная информационная система\n" +
                "планирования и учёта мероприятий.\n\n" +
                "Реализовано: направления, мероприятия, участники, отчёты.",
                "О программе",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (reminderTimer != null)
            {
                reminderTimer.Stop();
            }

            SaveData();
            base.OnFormClosing(e);
        }

        // ----------------------------------------------------------- helpers

        private static T Selected<T>(DataGridView grid) where T : class
        {
            if (grid.CurrentRow == null)
            {
                return null;
            }

            return grid.CurrentRow.DataBoundItem as T;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            return grid;
        }

        private static void AddColumn(DataGridView grid, string header, string property, int weight)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                DataPropertyName = property,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, AutoSize = true, Location = new Point(x, y) };
        }

        private static TextBox MakeText(int x, int y, int width)
        {
            return new TextBox { Location = new Point(x, y), Width = width };
        }

        private static Button MakeButton(string text, int x, int y, int width)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 30) };
        }
    }
}

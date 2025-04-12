using System;
using System.Drawing;
using System.Windows.Forms;
using static SnakeGame.SnakeGameLogic;

namespace SnakeGame
{
    public class MainMenuForm : Form
    {
        private ComboBox _levelComboBox;
        private CheckBox _wallsCheckBox;
        private Label _titleLabel;

        public MainMenuForm()
        {
            InitializeForm();
            InitializeControls();
        }

        private void InitializeForm()
        {
            this.Text = "Змейка - Главное меню";
            this.ClientSize = new Size(500, 550);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.LightGray;
            this.Padding = new Padding(20);
        }

        private void InitializeControls()
        {
            _titleLabel = new Label()
            {
                Text = "ЗМЕЙКА",
                Font = new Font("Arial", 28, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                AutoSize = true
            };
            _titleLabel.Location = new Point(
                (this.ClientSize.Width - _titleLabel.Width) / 2,
                20);

            var classicButton = new Button()
            {
                Text = "Классический режим",
                Size = new Size(250, 40),
                Location = new Point(125, 80),
                Font = new Font("Arial", 12),
                BackColor = Color.White
            };

            var survivalButton = new Button()
            {
                Text = "Режим выживания",
                Size = new Size(250, 40),
                Location = new Point(125, 130),
                Font = new Font("Arial", 12),
                BackColor = Color.White
            };

            var hardcoreButton = new Button()
            {
                Text = "ХАРДКОР РЕЖИМ",
                Size = new Size(250, 40),
                Location = new Point(125, 180),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 100, 100),
                ForeColor = Color.White
            };

            var aboutButton = new Button()
            {
                Text = "Об игре",
                Size = new Size(250, 40),
                Location = new Point(125, 230),
                Font = new Font("Arial", 12),
                BackColor = Color.White
            };

            var exitButton = new Button()
            {
                Text = "Выход",
                Size = new Size(250, 40),
                Location = new Point(125, 400),
                Font = new Font("Arial", 12),
                BackColor = Color.White
            };

            _levelComboBox = new ComboBox()
            {
                Location = new Point(200, 280),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 10)
            };

            var levelLabel = new Label()
            {
                Text = "Выберите уровень:",
                Location = new Point(50, 283),
                AutoSize = true,
                Font = new Font("Arial", 10)
            };

            _wallsCheckBox = new CheckBox()
            {
                Text = "С препятствиями",
                Location = new Point(200, 310),
                AutoSize = true,
                Font = new Font("Arial", 10)
            };

            classicButton.Click += (s, e) => StartGame(GameMode.Classic);
            survivalButton.Click += (s, e) => StartGame(GameMode.Survival);
            hardcoreButton.Click += (s, e) => StartGame(GameMode.Hardcore);
            aboutButton.Click += ShowAboutInfo;
            exitButton.Click += (s, e) => Application.Exit();

            for (int i = 1; i <= SnakeGameLogic.MaxUnlockedLevel; i++)
            {
                _levelComboBox.Items.Add($"Уровень {i}");
            }

            if (_levelComboBox.Items.Count > 0)
            {
                _levelComboBox.SelectedIndex = _levelComboBox.Items.Count - 1;
            }

            this.Controls.Add(_titleLabel);
            this.Controls.Add(classicButton);
            this.Controls.Add(survivalButton);
            this.Controls.Add(hardcoreButton);
            this.Controls.Add(aboutButton);
            this.Controls.Add(exitButton);
            this.Controls.Add(levelLabel);
            this.Controls.Add(_levelComboBox);
            this.Controls.Add(_wallsCheckBox);
        }

        private void ShowAboutInfo(object sender, EventArgs e)
        {
            string aboutText = @"Змейка - классическая аркадная игра.

Цель: Управляйте змейкой, собирайте яблоки и избегайте столкновений.

Бонусы:
- Синий: Ускорение (временно увеличивает скорость)
- Фиолетовый: Замедление (временно уменьшает скорость)
- Золотое яблоко: Дополнительные очки (+50 в классическом режиме /+25 в хардкорном режиме)

Управление:
- WASD - движение
- Ctrl - пауза
- F11 - полный экран
- ESC - выход
- R - рестарт";

            MessageBox.Show(aboutText, "О игре", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StartGame(GameMode mode)
        {
            int startLevel = mode == GameMode.Classic ? _levelComboBox.SelectedIndex + 1 : 1;
            bool withWalls = _wallsCheckBox.Checked || mode == GameMode.Hardcore;
            bool isHardcore = mode == GameMode.Hardcore;

            this.Hide();
            var gameForm = new SnakeGameForm(mode, startLevel, withWalls, isHardcore);
            gameForm.FormClosed += (s, e) => {
                if (!isHardcore && mode == GameMode.Classic)
                {
                    _levelComboBox.Items.Clear();
                    for (int i = 1; i <= SnakeGameLogic.MaxUnlockedLevel; i++)
                        _levelComboBox.Items.Add($"Уровень {i}");
                    if (_levelComboBox.Items.Count > 0)
                        _levelComboBox.SelectedIndex = _levelComboBox.Items.Count - 1;
                }
                this.Show();
            };
            gameForm.Show();
        }
    }
}
using System;
using System.Drawing;
using System.Windows.Forms;
using static SnakeGame.SnakeGameLogic;

namespace SnakeGame
{
    public class SnakeGameForm : Form
    {
        private readonly SnakeGameViewModel _viewModel;
        private System.Windows.Forms.Timer _gameTimer;
        private int _currentCellSize;
        private Label _scoreLabel;
        private Label _levelLabel;
        private Label _modeLabel;
        private Label _bonusLabel;
        private Button _menuButton;
        private Button _fullScreenButton;
        private int _startLevel;
        private bool _withWalls;
        private bool _fullScreen = false;
        private bool _isHardcore;
        public int MaxReachedLevel { get; private set; }

        public SnakeGameForm(GameMode mode, int startLevel, bool withWalls, bool isHardcore)
        {
            _startLevel = startLevel;
            MaxReachedLevel = startLevel;
            _withWalls = withWalls || isHardcore;
            _isHardcore = isHardcore;
            _currentCellSize = CalculateInitialCellSize(startLevel);
            _viewModel = new SnakeGameViewModel(mode, startLevel, withWalls, isHardcore);

            InitializeForm(mode);
            InitializeControls();
            InitializeGame();
        }

        private int CalculateInitialCellSize(int level)
        {
            int baseSize = 30 - level;
            return Math.Max(baseSize, 10);
        }

        private void InitializeForm(GameMode mode)
        {
            this.Text = $"Змейка - {mode}" + (_isHardcore ? " (ХАРДКОР)" : "");
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.DoubleBuffered = true;
            this.ClientSize = new Size(600, 600);
        }

        private void ToggleFullScreen()
        {
            _fullScreen = !_fullScreen;
            _fullScreenButton.Text = _fullScreen ? "Окно" : "Полный экран";
            UpdateWindowSize();
            this.Invalidate();
        }

        private void UpdateWindowSize()
        {
            if (_viewModel == null) return;

            int statusPanelHeight = 80;
            int minCellSize = 15;
            int maxCellSize = 30;
            int padding = 40;

            if (_fullScreen)
            {
                this.WindowState = FormWindowState.Maximized;
                this.FormBorderStyle = FormBorderStyle.None;

                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                int cellWidth = (screenWidth - padding) / _viewModel.Game.FieldWidth;
                int cellHeight = (screenHeight - statusPanelHeight - padding) / _viewModel.Game.FieldHeight;
                _currentCellSize = Math.Min(Math.Max(minCellSize, Math.Min(cellWidth, cellHeight)), maxCellSize);
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;

                _currentCellSize = Math.Max(minCellSize,
                    Math.Min(maxCellSize, 800 / Math.Max(_viewModel.Game.FieldWidth, _viewModel.Game.FieldHeight)));

                int width = _viewModel.Game.FieldWidth * _currentCellSize + padding;
                int height = _viewModel.Game.FieldHeight * _currentCellSize + statusPanelHeight + padding;

                this.MinimumSize = new Size(300, 300);
                this.MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
                this.ClientSize = new Size(
                    Math.Min(width, Screen.PrimaryScreen.WorkingArea.Width),
                    Math.Min(height, Screen.PrimaryScreen.WorkingArea.Height));
            }
        }

        private void InitializeControls()
        {
            var statusPanel = new Panel()
            {
                Size = new Size(this.ClientSize.Width, 80),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };

            _scoreLabel = new Label()
            {
                Location = new Point(15, 10),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            _levelLabel = new Label()
            {
                Location = new Point(15, 35),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };

            _modeLabel = new Label()
            {
                Location = new Point(15, 60),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.Purple
            };

            _bonusLabel = new Label()
            {
                Location = new Point(200, 10),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Italic),
                ForeColor = Color.OrangeRed
            };

            _menuButton = new Button()
            {
                Text = "Меню",
                Size = new Size(90, 30),
                Location = new Point(this.ClientSize.Width - 200, 10),
                Font = new Font("Arial", 10),
                BackColor = Color.LightSteelBlue,
                FlatStyle = FlatStyle.Flat
            };
            _menuButton.Click += (s, e) => this.Close();

            _fullScreenButton = new Button()
            {
                Text = "Полный экран",
                Size = new Size(90, 30),
                Location = new Point(this.ClientSize.Width - 100, 10),
                Font = new Font("Arial", 10),
                BackColor = Color.LightSteelBlue,
                FlatStyle = FlatStyle.Flat
            };
            _fullScreenButton.Click += (s, e) => ToggleFullScreen();

            statusPanel.Controls.AddRange(new Control[] {
                _scoreLabel, _levelLabel, _modeLabel, _bonusLabel,
                _menuButton, _fullScreenButton
            });

            this.Controls.Add(statusPanel);
        }

        private void InitializeGame()
        {
            _viewModel.OnGameUpdated += UpdateGameView;
            _viewModel.OnGameOver += ShowGameOver;
            _viewModel.Game.OnLevelChanged += UpdateFieldSize;
            EventBus.OnBonusActivated += ShowBonusMessage;

            _gameTimer = new System.Windows.Forms.Timer();
            _gameTimer.Interval = _viewModel.Game.IsHardcore ? 130 : 150;
            _gameTimer.Tick += (s, e) => _viewModel.Game.Update();
            _gameTimer.Start();

            this.KeyDown += HandleKeyPress;
            this.Paint += DrawGame;

            UpdateWindowSize();
            UpdateGameView();
        }

        private void UpdateFieldSize()
        {
            if (_viewModel.Game.Level > MaxReachedLevel)
            {
                MaxReachedLevel = _viewModel.Game.Level;
            }
            UpdateWindowSize();
            this.Invalidate();
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                    _viewModel.ChangeDirection(Direction.Up);
                    break;
                case Keys.S:
                    _viewModel.ChangeDirection(Direction.Down);
                    break;
                case Keys.A:
                    _viewModel.ChangeDirection(Direction.Left);
                    break;
                case Keys.D:
                    _viewModel.ChangeDirection(Direction.Right);
                    break;
                case Keys.ControlKey:
                    _viewModel.TogglePause();
                    break;
                case Keys.Escape:
                    if (_fullScreen)
                        ToggleFullScreen();
                    else
                        this.Close();
                    break;
                case Keys.R:
                    ResetGame();
                    break;
                case Keys.F11:
                    ToggleFullScreen();
                    break;
            }
        }

        private void DrawGame(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.Black);

            if (_viewModel == null || _viewModel.Game == null) return;

            int fieldWidth = _viewModel.Game.FieldWidth * _currentCellSize;
            int fieldHeight = _viewModel.Game.FieldHeight * _currentCellSize;
            int fieldX = (this.ClientSize.Width - fieldWidth) / 2;
            int fieldY = 80 + (this.ClientSize.Height - 80 - fieldHeight) / 2;

            using (var borderPen = new Pen(Color.Gray, 4))
            {
                g.DrawRectangle(borderPen, fieldX - 2, fieldY - 2,
                              fieldWidth + 4, fieldHeight + 4);
            }

            g.FillRectangle(Brushes.White, fieldX, fieldY, fieldWidth, fieldHeight);

            if (_viewModel.Game.Walls != null && _viewModel.Game.Walls.Count > 0)
            {
                using (var wallBrush = new SolidBrush(Color.FromArgb(70, 70, 70)))
                {
                    foreach (var wall in _viewModel.Game.Walls)
                    {
                        g.FillRectangle(wallBrush,
                            fieldX + wall.X * _currentCellSize,
                            fieldY + wall.Y * _currentCellSize,
                            _currentCellSize,
                            _currentCellSize);
                    }
                }
            }

            // Отрисовка бонусов
            if (_viewModel.Game.ActiveBonus != BonusType.None)
            {
                if (_viewModel.Game.ActiveBonus == BonusType.ExtraPoints)
                {
                    DrawGoldenApple(g,
                        fieldX + _viewModel.Game.BonusPosition.X * _currentCellSize,
                        fieldY + _viewModel.Game.BonusPosition.Y * _currentCellSize,
                        _currentCellSize);
                }
                else
                {
                    var bonusColor = _viewModel.Game.ActiveBonus == BonusType.SpeedUp ? Color.Blue : Color.Purple;
                    using (var bonusBrush = new SolidBrush(bonusColor))
                    {
                        int bonusSize = (int)(_currentCellSize * 0.8);
                        int bonusOffset = (_currentCellSize - bonusSize) / 2;
                        g.FillEllipse(bonusBrush,
                            fieldX + _viewModel.Game.BonusPosition.X * _currentCellSize + bonusOffset,
                            fieldY + _viewModel.Game.BonusPosition.Y * _currentCellSize + bonusOffset,
                            bonusSize,
                            bonusSize);
                    }
                }
            }

            // Отрисовка обычных яблок
            foreach (var food in _viewModel.Game.FoodPositions)
            {
                DrawApple(g, fieldX + food.X * _currentCellSize, fieldY + food.Y * _currentCellSize, _currentCellSize);
            }

            // Отрисовка змейки
            int segmentSize = Math.Max(_currentCellSize - 2, 5);
            using (var bodyBrush = new SolidBrush(_isHardcore ? Color.DarkRed : Color.Green))
            using (var headBrush = new SolidBrush(_isHardcore ? Color.Red : Color.DarkGreen))
            {
                foreach (var segment in _viewModel.Game.SnakeBody)
                {
                    g.FillRectangle(bodyBrush,
                        fieldX + segment.X * _currentCellSize,
                        fieldY + segment.Y * _currentCellSize,
                        segmentSize,
                        segmentSize);
                }

                if (_viewModel.Game.SnakeBody.Count > 0)
                {
                    g.FillRectangle(headBrush,
                        fieldX + _viewModel.Game.SnakeBody[0].X * _currentCellSize,
                        fieldY + _viewModel.Game.SnakeBody[0].Y * _currentCellSize,
                        segmentSize,
                        segmentSize);
                }
            }

            if (_viewModel.Game.IsPaused)
            {
                var pauseFont = new Font("Arial", 32, FontStyle.Bold);
                var pauseText = "ПАУЗА";
                var textSize = g.MeasureString(pauseText, pauseFont);

                using (var shadowBrush = new SolidBrush(Color.FromArgb(100, Color.Black)))
                {
                    g.DrawString(pauseText, pauseFont, shadowBrush,
                        (this.ClientSize.Width - textSize.Width) / 2 + 3,
                        (this.ClientSize.Height - textSize.Height) / 2 + 3);
                }

                g.DrawString(pauseText, pauseFont, Brushes.White,
                    (this.ClientSize.Width - textSize.Width) / 2,
                    (this.ClientSize.Height - textSize.Height) / 2);
            }
        }

        private void DrawApple(Graphics g, int x, int y, int cellSize)
        {
            int appleSize = (int)(cellSize * 0.8);
            int appleOffset = (cellSize - appleSize) / 2;

            using (var appleBrush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(appleBrush,
                    x + appleOffset,
                    y + appleOffset,
                    appleSize,
                    appleSize);
            }

            int leafSize = appleSize / 3;
            Point[] leafPoints = new Point[]
            {
                new Point(x + appleSize/2 + appleOffset, y + appleOffset - leafSize/2),
                new Point(x + appleSize + appleOffset - 2, y + appleOffset + leafSize/2),
                new Point(x + appleSize/2 + appleOffset, y + appleOffset + leafSize/2)
            };

            using (var leafBrush = new SolidBrush(Color.Green))
            {
                g.FillPolygon(leafBrush, leafPoints);
            }

            using (var stemPen = new Pen(Color.Brown, 2))
            {
                g.DrawLine(stemPen,
                    x + appleSize / 2 + appleOffset, y + appleOffset,
                    x + appleSize / 2 + appleOffset, y + appleOffset - leafSize / 2);
            }
        }

        private void DrawGoldenApple(Graphics g, int x, int y, int cellSize)
        {
            int appleSize = (int)(cellSize * 0.8);
            int appleOffset = (cellSize - appleSize) / 2;

            using (var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Point(x + appleOffset, y + appleOffset),
                new Point(x + appleOffset + appleSize, y + appleOffset + appleSize),
                Color.Gold,
                Color.Goldenrod))
            {
                g.FillEllipse(gradientBrush,
                    x + appleOffset,
                    y + appleOffset,
                    appleSize,
                    appleSize);
            }

            int leafSize = appleSize / 3;
            Point[] leafPoints = new Point[]
            {
                new Point(x + appleSize/2 + appleOffset, y + appleOffset - leafSize/2),
                new Point(x + appleSize + appleOffset - 2, y + appleOffset + leafSize/2),
                new Point(x + appleSize/2 + appleOffset, y + appleOffset + leafSize/2)
            };

            using (var leafBrush = new SolidBrush(Color.DarkGreen))
            {
                g.FillPolygon(leafBrush, leafPoints);
            }

            using (var stemPen = new Pen(Color.SaddleBrown, 2))
            {
                g.DrawLine(stemPen,
                    x + appleSize / 2 + appleOffset, y + appleOffset,
                    x + appleSize / 2 + appleOffset, y + appleOffset - leafSize / 2);
            }

            using (var shineBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            {
                int shineSize = appleSize / 3;
                g.FillEllipse(shineBrush,
                    x + appleOffset + appleSize / 3,
                    y + appleOffset + appleSize / 4,
                    shineSize,
                    shineSize / 2);
            }
        }

        private void UpdateGameView()
        {
            _scoreLabel.Text = $"Счет: {_viewModel.Game.Score}";
            _levelLabel.Text = $"Уровень: {_viewModel.Game.Level} (нужно {_viewModel.Game.Level * 50} очков)";
            _modeLabel.Text = $"Режим: {_viewModel.Game.Mode}" + (_isHardcore ? " (ХАРДКОР)" : "");

            if (_viewModel.Game.ActiveBonus != BonusType.None)
            {
                string bonusText = _viewModel.Game.ActiveBonus == BonusType.SpeedUp ? "Ускорение!" :
                                  _viewModel.Game.ActiveBonus == BonusType.SlowDown ? "Замедление!" :
                                  "Золотое яблоко! +" + (_isHardcore ? "25" : "50") + " очков";
                _bonusLabel.Text = bonusText;
            }
            else
            {
                _bonusLabel.Text = "";
            }

            this.Invalidate();
        }

        private void ShowBonusMessage(BonusType bonus)
        {
            string bonusText = bonus == BonusType.SpeedUp ? "Бонус: Ускорение!" :
                              bonus == BonusType.SlowDown ? "Бонус: Замедление!" :
                              "Золотое яблоко! +" + (_isHardcore ? "25" : "50") + " очков";
            _bonusLabel.Text = bonusText;
        }

        private void ShowGameOver()
        {
            _gameTimer.Stop();

            var result = MessageBox.Show($"Игра окончена! Счет: {_viewModel.Game.Score}\nХотите сыграть еще?",
                "Конец игры", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
                ResetGame();
            else
                this.Close();
        }

        private void ResetGame()
        {
            _gameTimer.Stop();
            _currentCellSize = CalculateInitialCellSize(_startLevel);
            _viewModel.ResetGame(_viewModel.Game.Mode, _startLevel, _withWalls, _isHardcore);
            _gameTimer.Start();
            UpdateWindowSize();
            UpdateGameView();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _gameTimer?.Stop();
            _gameTimer?.Dispose();
            EventBus.OnBonusActivated -= ShowBonusMessage;
        }
    }
}
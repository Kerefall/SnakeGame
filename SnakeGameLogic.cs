using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace SnakeGame
{
    [Serializable]
    public class SnakeGameLogic
    {
        public List<Point> SnakeBody { get; private set; }
        public List<Point> FoodPositions { get; private set; }
        public Point BonusPosition { get; private set; }
        public BonusType ActiveBonus { get; private set; }
        public int Score { get; private set; }
        public int Level { get; private set; }
        public bool IsPaused { get; private set; }
        public Direction CurrentDirection { get; private set; }
        public List<Point> Walls { get; private set; }
        public int FieldWidth { get; private set; }
        public int FieldHeight { get; private set; }
        public GameMode Mode { get; private set; }
        public bool IsHardcore { get; private set; }

        private Direction _nextDirection;
        private readonly Random _random;
        private const int PointsToNextLevel = 50;
        private int _speedIncreaseCounter;
        private int _initialSpeed = 150;
        private int _bonusTimer;
        private int _bonusCooldown;
        private bool _withWalls;
        private int _initialSnakeLength;

        [Serializable]
        private class GameProgress
        {
            public int MaxUnlockedLevel { get; set; }
            public Dictionary<int, LevelData> LevelData { get; set; } = new();
            public int HardcoreHighScore { get; set; }
            public int SurvivalHighScore { get; set; }
        }

        [Serializable]
        private class LevelData
        {
            public int SnakeLength { get; set; }
            public int Score { get; set; }
        }

        private static GameProgress _progress = new() { MaxUnlockedLevel = 1 };

        public static int MaxUnlockedLevel => _progress.MaxUnlockedLevel;
        public static int HardcoreHighScore => _progress.HardcoreHighScore;
        public static int SurvivalHighScore => _progress.SurvivalHighScore;

        public event Action OnGameUpdated;
        public event Action OnGameOver;
        public event Action OnLevelChanged;

        public SnakeGameLogic(GameMode mode, int startLevel = 1, bool withWalls = false, bool isHardcore = false)
        {
            _progress = LoadProgress();
            Mode = mode;
            IsHardcore = isHardcore;
            _withWalls = withWalls || isHardcore;
            _random = new Random();

            // В хардкоре всегда начинаем с 1 уровня
            if (isHardcore)
            {
                startLevel = 1;
                _initialSnakeLength = 3;
                Score = 0;
            }
            else if (mode == GameMode.Survival)
            {
                startLevel = 1;
                _initialSnakeLength = 3;
                Score = 0;
            }
            else
            {
                startLevel = Math.Min(startLevel, _progress.MaxUnlockedLevel);
                if (_progress.LevelData.TryGetValue(startLevel, out var levelData))
                {
                    _initialSnakeLength = levelData.SnakeLength;
                    Score = levelData.Score;
                }
                else
                {
                    _initialSnakeLength = 3 + (int)(startLevel * 0.5);
                    Score = 0;
                }
            }

            Level = startLevel;

            // Настройки размера поля для разных режимов
            if (isHardcore)
            {
                // Хардкор: +2 клетки каждые 2 уровня
                FieldWidth = 20 + ((startLevel - 1) / 2) * 2;
                FieldHeight = 20 + ((startLevel - 1) / 2) * 2;
            }
            else if (mode == GameMode.Survival)
            {
                // Выживание: +1 клетка каждый уровень
                FieldWidth = 20 + (startLevel - 1) * 1;
                FieldHeight = 20 + (startLevel - 1) * 1;
            }
            else
            {
                // Классический: +2 клетки каждый уровень
                FieldWidth = 20 + (startLevel - 1) * 2;
                FieldHeight = 20 + (startLevel - 1) * 2;
            }

            SnakeBody = new List<Point>();
            FoodPositions = new List<Point>();
            CurrentDirection = Direction.Right;
            _nextDirection = Direction.Right;
            IsPaused = false;
            ActiveBonus = BonusType.None;
            _bonusCooldown = _random.Next(50, 100);

            InitializeSnake();
            PlaceFood();

            if (_withWalls || Mode == GameMode.WithWalls || isHardcore)
                GenerateWalls(isHardcore);
            else
                Walls = new List<Point>();
        }

        private void InitializeSnake()
        {
            SnakeBody.Clear();
            int startX = FieldWidth / 2;
            int startY = FieldHeight / 2;

            for (int i = 0; i < _initialSnakeLength; i++)
                SnakeBody.Add(new Point(startX - i, startY));
        }

        private void GenerateWalls(bool hardcore = false)
        {
            Walls = new List<Point>();
            if (FieldWidth <= 0 || FieldHeight <= 0) return;

            int minWalls = hardcore ? 20 : 4;
            int maxWalls = hardcore ? 40 : 15;
            int wallCount = hardcore ? maxWalls : minWalls + (int)((maxWalls - minWalls) * (Level - 1) / 10.0);
            wallCount = Math.Min(wallCount, maxWalls);

            int safeZone = hardcore ? 2 : 5;
            int startX = Math.Max(0, SnakeBody[0].X - safeZone);
            int endX = Math.Min(FieldWidth - 1, SnakeBody[0].X + safeZone);
            int startY = Math.Max(0, SnakeBody[0].Y - safeZone);
            int endY = Math.Min(FieldHeight - 1, SnakeBody[0].Y + safeZone);

            for (int i = 0; i < wallCount; i++)
            {
                Point wall;
                int attempts = 0;
                do
                {
                    wall = new Point(
                        _random.Next(1, FieldWidth - 1),
                        _random.Next(1, FieldHeight - 1));
                    attempts++;
                }
                while ((wall.X >= startX && wall.X <= endX &&
                       wall.Y >= startY && wall.Y <= endY) ||
                       IsPositionOccupied(wall) && attempts < 100);

                if (attempts < 100)
                    Walls.Add(wall);
            }
        }

        private bool IsPositionOccupied(Point position)
        {
            return SnakeBody.Contains(position) ||
                   (Walls != null && Walls.Contains(position)) ||
                   FoodPositions.Contains(position) ||
                   position == BonusPosition;
        }

        private void PlaceFood()
        {
            int foodCount = Math.Min(1 + Level / 3, 5);

            while (FoodPositions.Count < foodCount)
            {
                var emptyCells = GetEmptyCells();
                if (emptyCells.Count > 0)
                {
                    var foodPos = emptyCells[_random.Next(emptyCells.Count)];
                    FoodPositions.Add(foodPos);
                }
                else
                {
                    break;
                }
            }
        }

        private void PlaceBonus()
        {
            var emptyCells = GetEmptyCells();
            if (emptyCells.Count > 0 && _bonusCooldown <= 0)
            {
                BonusPosition = emptyCells[_random.Next(emptyCells.Count)];
                ActiveBonus = (BonusType)_random.Next(1, 4);
                _bonusTimer = 50 + _random.Next(0, 50);
                EventBus.PublishBonus(ActiveBonus);
            }
        }

        private List<Point> GetEmptyCells()
        {
            var emptyCells = new List<Point>();
            for (int x = 0; x < FieldWidth; x++)
            {
                for (int y = 0; y < FieldHeight; y++)
                {
                    var point = new Point(x, y);
                    if (!IsPositionOccupied(point))
                    {
                        emptyCells.Add(point);
                    }
                }
            }
            return emptyCells;
        }

        public void ChangeDirection(Direction newDirection)
        {
            if ((CurrentDirection == Direction.Up && newDirection != Direction.Down) ||
                (CurrentDirection == Direction.Down && newDirection != Direction.Up) ||
                (CurrentDirection == Direction.Left && newDirection != Direction.Right) ||
                (CurrentDirection == Direction.Right && newDirection != Direction.Left))
            {
                _nextDirection = newDirection;
            }
        }

        public void Update()
        {
            if (IsPaused) return;

            if (ActiveBonus != BonusType.None)
            {
                _bonusTimer--;
                if (_bonusTimer <= 0)
                {
                    ActiveBonus = BonusType.None;
                    _bonusCooldown = _random.Next(50, 100);
                    BonusPosition = new Point(-1, -1);
                }
            }
            else
            {
                _bonusCooldown--;
                if (_bonusCooldown <= 0 && _random.Next(0, 100) < 5)
                {
                    PlaceBonus();
                }
            }

            if (Mode == GameMode.Survival)
            {
                _speedIncreaseCounter++;
                if (_speedIncreaseCounter % 20 == 0 && _initialSpeed > 50)
                {
                    _initialSpeed -= 2;
                }
            }

            CurrentDirection = _nextDirection;
            MoveSnake();
            CheckCollisions();
            OnGameUpdated?.Invoke();
        }

        private void MoveSnake()
        {
            var head = SnakeBody[0];
            var newHead = new Point(head.X, head.Y);

            switch (CurrentDirection)
            {
                case Direction.Up: newHead.Y--; break;
                case Direction.Down: newHead.Y++; break;
                case Direction.Left: newHead.X--; break;
                case Direction.Right: newHead.X++; break;
            }

            SnakeBody.Insert(0, newHead);

            if (ActiveBonus != BonusType.None && newHead == BonusPosition)
            {
                ApplyBonusEffect();
                ActiveBonus = BonusType.None;
                BonusPosition = new Point(-1, -1);
                _bonusCooldown = _random.Next(50, 100);
            }

            bool ateFood = false;
            for (int i = 0; i < FoodPositions.Count; i++)
            {
                if (newHead == FoodPositions[i])
                {
                    Score += IsHardcore ? 20 : 10;
                    ateFood = true;
                    FoodPositions.RemoveAt(i);
                    break;
                }
            }

            if (ateFood)
            {
                if (Mode == GameMode.Classic && Score >= Level * PointsToNextLevel)
                {
                    LevelUp();
                }
                PlaceFood();
            }
            else
            {
                SnakeBody.RemoveAt(SnakeBody.Count - 1);
            }
        }

        private void ApplyBonusEffect()
        {
            switch (ActiveBonus)
            {
                case BonusType.SpeedUp:
                    _initialSpeed = Math.Max(30, _initialSpeed - (IsHardcore ? 200 : 160));
                    break;
                case BonusType.SlowDown:
                    _initialSpeed += (IsHardcore ? 200 : 160);
                    break;
                case BonusType.ExtraPoints:
                    Score += IsHardcore ? 100 : 50;
                    if (Mode == GameMode.Classic && Score >= Level * PointsToNextLevel)
                    {
                        LevelUp();
                    }
                    break;
            }
        }

        private void LevelUp()
        {
            // В режиме выживания и хардкора сохраняем только рекорд
            if (Mode == GameMode.Classic)
            {
                _progress.LevelData[Level] = new LevelData
                {
                    SnakeLength = SnakeBody.Count,
                    Score = Score
                };
            }

            Level++;

            // Увеличение карты в зависимости от режима
            if (IsHardcore)
            {
                // Хардкор: +2 клетки каждые 2 уровня
                if (Level % 2 == 1)
                {
                    FieldWidth += 2;
                    FieldHeight += 2;
                }
            }
            else if (Mode == GameMode.Survival)
            {
                // Выживание: +1 клетка каждый уровень
                FieldWidth += 1;
                FieldHeight += 1;
            }
            else
            {
                // Классический: +2 клетки каждый уровень
                FieldWidth += 2;
                FieldHeight += 2;
            }

            _initialSnakeLength = SnakeBody.Count;

            if (Mode == GameMode.Classic && Level > _progress.MaxUnlockedLevel)
            {
                _progress.MaxUnlockedLevel = Level;
                SaveProgress();
            }

            if (_withWalls || Mode == GameMode.WithWalls || IsHardcore)
                GenerateWalls(IsHardcore);

            InitializeSnake();
            PlaceFood();
            OnLevelChanged?.Invoke();
        }

        private void CheckCollisions()
        {
            var head = SnakeBody[0];

            bool wallCollision = (_withWalls || Mode == GameMode.WithWalls || IsHardcore) &&
                               Walls != null &&
                               Walls.Contains(head);

            bool borderCollision = head.X < 0 || head.X >= FieldWidth ||
                                 head.Y < 0 || head.Y >= FieldHeight;

            if (wallCollision || borderCollision)
            {
                HandleGameOver();
                return;
            }

            for (int i = 1; i < SnakeBody.Count; i++)
            {
                if (head == SnakeBody[i])
                {
                    HandleGameOver();
                    return;
                }
            }
        }

        private void HandleGameOver()
        {
            if (IsHardcore && Score > _progress.HardcoreHighScore)
            {
                _progress.HardcoreHighScore = Score;
                SaveProgress();
            }
            else if (Mode == GameMode.Survival && Score > _progress.SurvivalHighScore)
            {
                _progress.SurvivalHighScore = Score;
                SaveProgress();
            }
            OnGameOver?.Invoke();
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            OnGameUpdated?.Invoke();
        }

        private static void SaveProgress()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_progress, options);
                File.WriteAllText("progress.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения прогресса: {ex.Message}");
            }
        }

        private static GameProgress LoadProgress()
        {
            try
            {
                if (File.Exists("progress.json"))
                {
                    string json = File.ReadAllText("progress.json");
                    return JsonSerializer.Deserialize<GameProgress>(json) ?? new GameProgress { MaxUnlockedLevel = 1 };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки прогресса: {ex.Message}");
            }
            return new GameProgress { MaxUnlockedLevel = 1 };
        }

        public static void UpdateMaxUnlockedLevel(int level)
        {
            if (level > _progress.MaxUnlockedLevel)
            {
                _progress.MaxUnlockedLevel = level;
                SaveProgress();
            }
        }

        public enum Direction { Up, Down, Left, Right }
        public enum GameMode { Classic, Survival, WithWalls, Hardcore }
        public enum BonusType { None, SpeedUp, SlowDown, ExtraPoints }
    }
}
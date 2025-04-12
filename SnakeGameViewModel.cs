using static SnakeGame.SnakeGameLogic;

namespace SnakeGame
{
    public class SnakeGameViewModel
    {
        private SnakeGameLogic _game;
        public SnakeGameLogic Game => _game;

        public event Action OnGameUpdated;
        public event Action OnGameOver;

        public SnakeGameViewModel(GameMode mode, int startLevel, bool withWalls, bool isHardcore = false)
        {
            InitializeGame(mode, startLevel, withWalls, isHardcore);
        }

        private void InitializeGame(GameMode mode, int startLevel, bool withWalls, bool isHardcore)
        {
            _game = new SnakeGameLogic(mode, startLevel, withWalls, isHardcore);
            _game.OnGameUpdated += () => OnGameUpdated?.Invoke();
            _game.OnGameOver += () => OnGameOver?.Invoke();
        }

        public void ResetGame(GameMode mode, int startLevel, bool withWalls, bool isHardcore)
        {
            InitializeGame(mode, startLevel, withWalls, isHardcore);
        }

        public void ChangeDirection(Direction dir) => _game.ChangeDirection(dir);
        public void TogglePause() => _game.TogglePause();
    }
}
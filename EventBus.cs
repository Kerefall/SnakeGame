using System;

namespace SnakeGame
{
    public static class EventBus
    {
        public static event Action<SnakeGameLogic.BonusType> OnBonusActivated;

        public static void PublishBonus(SnakeGameLogic.BonusType bonus)
        {
            OnBonusActivated?.Invoke(bonus);
        }
    }
}
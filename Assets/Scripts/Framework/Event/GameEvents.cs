namespace GameEvents
{
    namespace Gameplay
    {
        public static class Events
        {
            public const string UpdateHPDisplay = "Gameplay.Player.UpdateHPDisplay";    // 血量发生变化后，通知其他地方同步该变化
        }
    }
}

namespace Project.Core.Visual.Games.Fishing
{
    public enum FishingVisualPhase
    {
        Idle,
        Waiting,
        Biting,
        Reeling,
        RoundEnd,
        Win
    }

    public enum FishingVisualFishAction
    {
        Idle,
        Move,
        Burst
    }

    public readonly struct FishingVisualState
    {
        public readonly bool Visible;
        public readonly bool Paused;

        public readonly FishingVisualPhase Phase;
        public readonly FishingVisualFishAction FishAction;

        public readonly bool FloatVisible;
        public readonly float FishDistance01;
        public readonly float FishLateral01;

        public readonly bool BiteIsOn;
        public readonly bool CanCatchNow;

        public readonly int TensionTicks;
        public readonly int TensionMaxTicks;
        public readonly int TensionWarnTick;

        public FishingVisualState(
            bool visible,
            bool paused,
            FishingVisualPhase phase,
            FishingVisualFishAction fishAction,
            bool floatVisible,
            float fishDistance01,
            float fishLateral01,
            bool biteIsOn,
            bool canCatchNow,
            int tensionTicks,
            int tensionMaxTicks,
            int tensionWarnTick
        )
        {
            Visible = visible;
            Paused = paused;

            Phase = phase;
            FishAction = fishAction;

            FloatVisible = floatVisible;
            FishDistance01 = fishDistance01;
            FishLateral01 = fishLateral01;

            BiteIsOn = biteIsOn;
            CanCatchNow = canCatchNow;

            TensionTicks = tensionTicks;
            TensionMaxTicks = tensionMaxTicks;
            TensionWarnTick = tensionWarnTick;
        }
    }
}
namespace DeenCraft.Player
{
    public sealed class FlightInput
    {
        private const float DoubleTapSeconds = 0.3f;
        private float _lastJumpTime = float.NegativeInfinity;

        public bool IsFlying { get; private set; }

        public bool PressJump(float time)
        {
            float elapsed = time - _lastJumpTime;
            if (elapsed >= 0f && elapsed <= DoubleTapSeconds)
            {
                IsFlying = !IsFlying;
                _lastJumpTime = float.NegativeInfinity;
                return true;
            }

            _lastJumpTime = time;
            return false;
        }
    }
}

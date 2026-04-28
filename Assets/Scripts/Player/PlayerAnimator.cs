// Assets/Scripts/Player/PlayerAnimator.cs
using UnityEngine;

namespace DeenCraft.Player
{
    /// <summary>
    /// Reads PlayerController state and drives Animator parameters.
    /// No-ops gracefully if Animator component is absent (capsule placeholder case).
    /// 
    /// Animator parameters expected:
    ///   float  Speed       (0=idle, 1=walk, 2=run)
    ///   bool   IsJumping
    ///   bool   IsSwimming
    ///   int    ActionState (PlayerAnimationState cast to int)
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedHash       = Animator.StringToHash("Speed");
        private static readonly int IsJumpingHash   = Animator.StringToHash("IsJumping");
        private static readonly int IsSwimmingHash  = Animator.StringToHash("IsSwimming");
        private static readonly int ActionStateHash = Animator.StringToHash("ActionState");

        private Animator         _animator;
        private PlayerController _controller;

        private void Awake()
        {
            _animator   = GetComponent<Animator>(); // null if no Animator — that's fine
            _controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_animator == null) return;

            float speed = 0f;
            if      (_controller.IsSprinting) speed = 2f;
            else if (_controller.IsMoving)    speed = 1f;

            _animator.SetFloat(SpeedHash,       speed);
            _animator.SetBool(IsJumpingHash,    _controller.IsJumping);
            _animator.SetBool(IsSwimmingHash,   _controller.IsSwimming);
            _animator.SetInteger(ActionStateHash, (int)_controller.CurrentAnimationState);
        }
    }
}

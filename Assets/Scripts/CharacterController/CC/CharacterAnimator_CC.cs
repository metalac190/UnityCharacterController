using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMotor_CC))]
public class CharacterAnimator_CC : MonoBehaviour
{
    const string IdleState = "Idle";
    const string RunState = "Run";
    const string JumpState = "Jumping";
    const string FallState = "Falling";

    bool _isRunning = false;

    CharacterMotor_CC _motor = null;
    Animator _animator = null;

    private void Awake()
    {
        _motor = GetComponent<CharacterMotor_CC>();
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _motor.StoppedMoving += OnStoppedMoving;
        _motor.StartedMoving += OnStartedMoving;
        _motor.Landed += OnLanded;
        _motor.JumpStarted += OnJumpStarted;
        _motor.DoubleJumpStarted += OnDoubleJumpStarted;
        _motor.FallStarted += OnFallStarted;
    }

    private void OnDisable()
    {
        _motor.StoppedMoving -= OnStoppedMoving;
        _motor.StartedMoving -= OnStartedMoving;
        _motor.Landed -= OnLanded;
        _motor.JumpStarted -= OnJumpStarted;
        _motor.DoubleJumpStarted -= OnDoubleJumpStarted;
        _motor.FallStarted -= OnFallStarted;
    }

    private void Start()
    {
        _animator.CrossFadeInFixedTime(IdleState, .2f);
    }

    void OnStoppedMoving()
    {
        if (!_motor.IsGrounded)
            return;

        _animator.CrossFadeInFixedTime(IdleState, .2f);
    }

    void OnStartedMoving()
    {
        // if we're in the air, don't change animations
        if (!_motor.IsGrounded)
            return;

        _animator.CrossFadeInFixedTime(RunState, .3f);
    }

    void OnLanded()
    {
        if (_motor.CurrentMomentumRatio >= .2f)
        {
            _animator.CrossFadeInFixedTime(RunState, .3f);
        }
        else if (_motor.CurrentMomentumRatio < .2f)
        {
            _animator.CrossFadeInFixedTime(IdleState, .3f);
        }
    }

    void OnJumpStarted()
    {
        _animator.CrossFadeInFixedTime(JumpState, .2f);
    }

    void OnDoubleJumpStarted()
    {
        _animator.CrossFadeInFixedTime(JumpState, .2f);
    }

    void OnFallStarted()
    {
        _animator.CrossFadeInFixedTime(FallState, .2f);
    }
}

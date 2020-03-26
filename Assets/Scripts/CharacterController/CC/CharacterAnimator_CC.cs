using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMotor_CC))]
public class CharacterAnimator_CC : MonoBehaviour
{
    [SerializeField] float _runningThreshold = .2f;

    [Header("Blend Time")]
    [SerializeField] float _stoppedMovingBlendTime = .2f;
    [SerializeField] float _startedMovingBlendTime = .2f;
    [SerializeField] float _landedBlendTime = .3f;
    [SerializeField] float _jumpBlendTime = .2f;
    [SerializeField] float _doubleJumpBlendTime = .2f;
    [SerializeField] float _fallBlendTime = .2f;

    const string IdleState = "Idle";
    const string RunState = "Run";
    const string JumpState = "Jumping";
    const string FallState = "Falling";

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
        _animator.CrossFadeInFixedTime(IdleState, 0);
    }

    void OnStoppedMoving()
    {
        // if we're in the air, continue current aerial animations
        if (!_motor.IsGrounded)
            return;

        _animator.CrossFadeInFixedTime(IdleState, _stoppedMovingBlendTime);
    }

    void OnStartedMoving()
    {
        // if we're in the air, continue current aerial animations
        if (!_motor.IsGrounded)
            return;

        _animator.CrossFadeInFixedTime(RunState, _startedMovingBlendTime);
    }

    void OnLanded()
    {
        // if moving, start running as soon as we land
        if (_motor.CurrentMomentumRatio >= _runningThreshold)
        {
            _animator.CrossFadeInFixedTime(RunState, _landedBlendTime);
        }
        // if not moving, idle as soon as we land
        else if (_motor.CurrentMomentumRatio < _runningThreshold)
        {
            _animator.CrossFadeInFixedTime(IdleState, _landedBlendTime);
        }
    }

    void OnJumpStarted()
    {
        _animator.CrossFadeInFixedTime(JumpState, _jumpBlendTime);
    }

    void OnDoubleJumpStarted()
    {
        _animator.CrossFadeInFixedTime(JumpState, _doubleJumpBlendTime);
    }

    void OnFallStarted()
    {
        _animator.CrossFadeInFixedTime(FallState, _fallBlendTime);
    }
}

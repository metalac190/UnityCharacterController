using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMotor))]
public class CharacterAnimator : MonoBehaviour
{
    const string XInputParam = "XInput";
    const string YInputParam = "YInput";
    const string SpeedParam = "Speed";

    const string JumpParam = "Jump";
    const string FallParam = "Fall";
    const string LandParam = "Land";


    CharacterMotor _motor = null;
    Animator _animator = null;

    Coroutine _triggerTimer;

    private void Awake()
    {
        _motor = GetComponent<CharacterMotor>();
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _motor.MovementChanged += OnMovementChanged;
        _motor.SpeedChanged += OnSpeedChanged;
        _motor.Landed += OnLanded;
        _motor.Jumped += OnJumped;
        _motor.StartedFalling += OnStartedFalling;
    }

    private void OnDisable()
    {
        _motor.MovementChanged -= OnMovementChanged;
        _motor.SpeedChanged -= OnSpeedChanged;
        _motor.Landed -= OnLanded;
        _motor.Jumped -= OnJumped;
        _motor.StartedFalling -= OnStartedFalling;
    }

    void OnMovementChanged(Vector2 movement)
    {
        //TODO remove and replace with ACTUAL vector2 movement later
        _animator.SetFloat(XInputParam, _motor.CurrentSpeedNormalized);
        _animator.SetFloat(YInputParam, _motor.CurrentSpeedNormalized);
        //Debug.Log(movement);
    }

    void OnSpeedChanged(float speed)
    {
        float normalizedSpeed = (1 / _motor.MaxSpeed) * speed;
        _animator.SetFloat(SpeedParam, normalizedSpeed);
    }

    void OnLanded()
    {
        //_animator.ResetTrigger(JumpParam);
        //_animator.ResetTrigger(FallParam);

        //_animator.SetTrigger(LandParam);
        SetTriggerQuick(LandParam);
        Debug.Log("Landed!");
    }

    void OnJumped()
    {
        //_animator.ResetTrigger(LandParam);
        //_animator.ResetTrigger(FallParam);

        //_animator.SetTrigger(JumpParam);
        SetTriggerQuick(JumpParam);
        Debug.Log("Jumped!");
    }

    void OnStartedFalling()
    {
        //_animator.ResetTrigger(LandParam);
        //_animator.ResetTrigger(JumpParam);

        //_animator.SetTrigger(FallParam);
        SetTriggerQuick(FallParam);
        Debug.Log("Started Falling!");
    }

    // wrote this because Mecanim SetTrigger is the worst
    void SetTriggerQuick(string triggerName)
    {
        _triggerTimer = StartCoroutine(SetTriggerQuickRoutine(triggerName));
    }

    IEnumerator SetTriggerQuickRoutine(string triggerName)
    {
        _animator.SetTrigger(triggerName);
        yield return null;
        _animator.ResetTrigger(triggerName);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMotor))]
public class CharacterAnimator : MonoBehaviour
{
    CharacterMotor _motor = null;
    Animator _animator = null;

    private void Awake()
    {
        _motor = GetComponent<CharacterMotor>();
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _motor.Landed += OnLanded;
        _motor.Jumped += OnJumped;
        _motor.StartedFalling += OnStartedFalling;
    }

    private void OnDisable()
    {
        _motor.Landed -= OnLanded;
        _motor.Jumped -= OnJumped;
        _motor.StartedFalling -= OnStartedFalling;
    }

    void OnLanded()
    {
        Debug.Log("Landed!");
    }

    void OnJumped()
    {
        Debug.Log("Jumped!");
    }

    void OnStartedFalling()
    {
        Debug.Log("Started Falling!");
    }
}

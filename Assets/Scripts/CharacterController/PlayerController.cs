using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public IPawn ActivePlayer { get; private set; }

    PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    public void Control(IPawn pawn)
    {
        pawn.OnControlled(_playerInput);
    }

    public void Release(IPawn pawn)
    {
        pawn.OnReleased(_playerInput);
    }
}

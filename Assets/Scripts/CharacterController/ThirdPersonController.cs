using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
public class ThirdPersonController : Pawn
{
    CharacterMotor _characterMotor;

    void Awake()
    {
        _characterMotor = GetComponent<CharacterMotor>();
    }

    // gets called if calling base.Control()
    public override void OnControlled()
    {
        Input.MoveInput += OnMoveInput;
        Input.Jump += OnJump;
    }

    // gets called if calling base.Release()
    public override void OnReleased()
    {
        // unhook from inputs
        Input.MoveInput -= OnMoveInput;
        Input.Jump -= OnJump;
    }

    void OnMoveInput(Vector2 movement)
    {
        // convert input into 3D direction
        Vector3 absoluteMovement = new Vector3(movement.x, 0, movement.y);
        // convert direction to be relative to camera orientation
        Vector3 localMovement = InputHelper.
            ConvertDirectionToCameraLocal(absoluteMovement, Camera.transform);
        // ensure y in unaffected by conversion
        localMovement.y = 0;

        _characterMotor.Move(localMovement);
        _characterMotor.Rotate(localMovement);
    }

    void OnJump()
    {
        _characterMotor.Jump();
    }
}

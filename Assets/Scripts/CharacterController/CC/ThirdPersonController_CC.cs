using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMotor_CC))]
public class ThirdPersonController_CC : Pawn
{
    CharacterMotor_CC _motor;

    private void Awake()
    {
        _motor = GetComponent<CharacterMotor_CC>();
    }

    public override void OnControlled()
    {
        Input.MoveInput += OnMoveInput;
        Input.Jump += OnJump;
        Input.Jumping += OnJumping;
    }

    public override void OnReleased()
    {
        // inputs unsubscribe
        Input.MoveInput -= OnMoveInput;
        Input.Jump -= OnJump;
        Input.Jumping -= OnJumping;
    }

    void OnMoveInput(Vector2 movement)
    {
        Vector3 absoluteMovement = new Vector3(movement.x, 0, movement.y);
        // convert direction to be relative to camera orientation
        Vector3 localMovement = InputHelper.
            ConvertDirectionToCameraLocal(absoluteMovement, Camera.transform);
        // ensure y in unaffected by conversion
        localMovement.y = 0;

        // move
        _motor.RequestMove(localMovement);
        // rotate
        _motor.RequestTurn(localMovement);


        /*
        // convert input into 3D direction
        Vector3 absoluteMovement = new Vector3(movement.x, 0, movement.y);
        // convert direction to be relative to camera orientation
        Vector3 localMovement = InputHelper.
            ConvertDirectionToCameraLocal(absoluteMovement, _camera.transform);
        // ensure y in unaffected by conversion
        localMovement.y = 0;

        //_characterMotor.Move(localMovement);
        //_characterMotor.Rotate(localMovement);
        */
    }

    void OnJump()
    {
        _motor.RequestJump();
    }

    void OnJumping()
    {
        _motor.RequestJumpContinuous();
    }
}

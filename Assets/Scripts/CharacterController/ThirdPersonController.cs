using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
public class ThirdPersonController : MonoBehaviour, IPawn
{
    CharacterMotor _characterMotor;
    // references while Controlled
    PlayerController _controller;
    PlayerInput _input;
    Camera _camera;

    void Awake()
    {
        _characterMotor = GetComponent<CharacterMotor>();
    }

    // if controlled, hook into input
    public void OnControlled(PlayerController controller, PlayerInput input, Camera camera)
    {
        _controller = controller;
        _input = input;
        _camera = camera;
        // hook into inputs
        input.MoveInput += OnMoveInput;
        input.Jump += OnJump;
    }

    // if released, forget input
    public void OnReleased(PlayerController controller, PlayerInput input, Camera camera)
    {
        // unhook from inputs
        _input.MoveInput -= OnMoveInput;
        _input.Jump -= OnJump;
        // clean up
        _controller = null;
        _input = null;
        _camera = null;
    }

    // if this gameObject is disabled, release it justin case
    void OnDisable()
    {
        if(_controller != null)
        {
            OnReleased(_controller, _input, _camera);
        }
    }

    void OnMoveInput(Vector2 movement)
    {
        // convert input into 3D direction
        Vector3 absoluteMovement = new Vector3(movement.x, 0, movement.y);
        // convert direction to be relative to camera orientation
        Vector3 localMovement = InputHelper.
            ConvertDirectionToCameraLocal(absoluteMovement, _camera);
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

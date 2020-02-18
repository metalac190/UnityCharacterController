using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
public class ThirdPersonController : MonoBehaviour, IPawn
{
    Vector2 _input;
    Vector3 _currentDirection;
    Quaternion _targetRotation;
    Rigidbody _rb;
    CharacterMotor _characterMotor;

    [SerializeField] Transform _camera;


    void Awake()
    {
        _characterMotor = GetComponent<CharacterMotor>();
    }

    void FixedUpdate()
    {
        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");

        // if we don't have input, don't send it
        if(_input.x == 0 && _input.y == 0)
        {
            return;
        }

        // we need to convert our input into our intended direction.
        // in this case, we want to move relative to the camera, 
        Vector3 moveDirection = GetTargetDirection();

        _characterMotor.Move(moveDirection);
        _characterMotor.Rotate(moveDirection);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _characterMotor.Jump();
        }
    }

    private Vector3 GetTargetDirection()
    {
        Vector3 horizontalMovement = _camera.transform.right * _input.x;
        Vector3 forwardMovement = _camera.transform.forward * _input.y;
        // get a combined direction with normalized length
        Vector3 moveDirection = (horizontalMovement + forwardMovement).normalized;
        moveDirection.y = 0;
        return moveDirection;
    }

    public void OnControlled(PlayerInput input)
    {
        // hook into inputs
    }

    public void OnReleased(PlayerInput input)
    {
        // hook into inputs
    }
}

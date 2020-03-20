using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Ground detect requires ground objects to be set to 'Ground' Layer (Layer #8)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float _maxSpeed = 5f;
    public float MaxSpeed
    {
        get => _maxSpeed;
        private set => _maxSpeed = value;
    }
    [SerializeField] float _accelToMaxInSec = .5f;
    [SerializeField] float _decelToZeroInSec = .3f;

    [Header("Rotation")]
    [SerializeField] float _turnSpeed = .15f;

    [Header("Jumping")]
    [SerializeField] float _jumpHeight = 3;
    [SerializeField] bool _allowDoubleJump = true;
    [SerializeField] float _doubleJumpForce = 150;
    [SerializeField] LayerMask _groundTestLayers = -1;  // default to 'everything'

    public event Action<Vector2> MovementChanged = delegate { };
    public event Action<float> SpeedChanged = delegate { };
    public event Action Jumped = delegate { };
    public event Action Landed = delegate { };
    public event Action StartedFalling = delegate { };
    public event Action KnockedBack = delegate { };

    // speed accounting for acceleration
    float _currentSpeed;
    public float CurrentSpeed 
    {
        get => _currentSpeed;
        private set
        {
            if(value != _currentSpeed)
            {
                SpeedChanged.Invoke(value);
            }
            _currentSpeed = value;
        }
    }
    // returns our current speed as a fraction of the max
    public float CurrentMomentumRatio
    {
        get => (1 / MaxSpeed) * _currentSpeed;
    }

    public float AccelRatePerSecond
    {
        get { return _maxSpeed / _accelToMaxInSec; }
    }
    public float DecelRatePerSecond
    {
        get { return _maxSpeed / _decelToZeroInSec; }
    }

    public bool IsFalling { get; private set; } = true;    // falling is defined as 'after the apex of the jump'
    public bool IsGrounded { get; private set; } = true;

    Rigidbody _rb;

    // movement
    Vector3 _requestedMovementThisFrame = Vector3.zero;
    Quaternion _requestedRotationThisFrame;

    // state booleans
    bool _doubleJumpReady = true;
    bool _jumpThisFrame = false;

    #region MonoBehaviour
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Reset()
    {
        // disable player layer for default setup. 
        int _playerLayer = 8;
        _groundTestLayers = ~(1 << _playerLayer);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            _rb.AddForce(transform.forward * 300);
        }
    }

    private void FixedUpdate()
    {
        // state checks
        CheckIfGrounded();
        CheckIfFalling();
        // calculate
        CalculateMomentum();
        // apply
        ApplyJump(_jumpThisFrame);
        ApplyMovement(_requestedMovementThisFrame);
        ApplyRotation(_requestedRotationThisFrame);
    }
    #endregion

    #region Public
    public void Move(Vector3 moveDirection)
    {
        _requestedMovementThisFrame = moveDirection;
    }

    public void Rotate(Vector3 moveDirection)
    {       
        _requestedRotationThisFrame = Quaternion.LookRotation(moveDirection);
    }

    public void Jump()
    {
        _jumpThisFrame = true;
    }
    #endregion

    #region Private Methods
    
    private void CalculateMomentum()
    {
        // if we're currently trying to move, accelerate
        if (_requestedMovementThisFrame != Vector3.zero)
        {
            CurrentSpeed += AccelRatePerSecond * Time.fixedDeltaTime;
        }
        // if there's no movement command, deaccelerate
        else
        {
            CurrentSpeed -= DecelRatePerSecond * Time.fixedDeltaTime;
        }
        // don't go over the max speed
        CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0, _maxSpeed);
        //float speedRatio = Mathf.Lerp(0, _maxSpeed, (1 / MaxSpeed) * _currentSpeed);
        Debug.Log("CurrentSpeed: " + CurrentSpeed);
    }

    void ApplyMovement(Vector3 newMovement)
    {
        // make sure we only allow more velocity, if we haven't hit max speed
        if (newMovement != Vector3.zero)
        {
            newMovement = newMovement * CurrentMomentumRatio;
        }
        else
        {
            newMovement = newMovement * -CurrentMomentumRatio;
        }
        Debug.Log("New Movement: " + newMovement);
        // FINALLY apply movement
        _rb.velocity += newMovement;
        Debug.Log("Current Velocity: " + _rb.velocity);
        //TODO actually call this when something moved, not every fixedUpdate
        MovementChanged.Invoke(newMovement);
        // clear out our movement
        _requestedMovementThisFrame = Vector3.zero;

    }

    void ApplyRotation(Quaternion rotationThisFrame)
    {
        rotationThisFrame.Normalize();
        Quaternion newRotation = Quaternion.Slerp(_rb.rotation, rotationThisFrame, _turnSpeed);
        _rb.MoveRotation(newRotation);
        // don't reset it, so that we maintain current facing direction
    }

    void ApplyJump(bool shouldJump)
    {
        // if we're on the ground, allow a jump
        if (IsGrounded && shouldJump == true)
        {
            _rb.AddForce(Vector3.up * Mathf.Sqrt(_jumpHeight * -2f 
                * Physics.gravity.y), ForceMode.VelocityChange);
            Jumped.Invoke();
        }
        // if we're in the air, capable of double jump, 
        // and have received jump command, allow double jump
        else
        {
            if (shouldJump && _allowDoubleJump && _doubleJumpReady)
            {
                _doubleJumpReady = false;
                // kill previous momentum before applying new jump
                _rb.velocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.x);
                _rb.AddForce(Vector3.up * Mathf.Sqrt(_doubleJumpForce * -2f
                    * Physics.gravity.y), ForceMode.VelocityChange);
                Jumped.Invoke();
            }
        }
        // reset it to false, so that we can receive a new jump command
        _jumpThisFrame = false;
    }

    void CheckIfFalling()
    {
        bool previouslyFalling = IsFalling;
        // if we have downward velocity and we're not grounded, then we're falling
        if(_rb.velocity.y < 0 && !IsGrounded)
        {
            // we're falling!
            // if we weren't falling before, but now we are, start falling state
            if (previouslyFalling == false)
            {
                StartedFalling.Invoke();
            }

            IsFalling = true;
        }
        else
        {
            IsFalling = false;
        }
    }

    private void CheckIfGrounded()
    {
        // store grounded before changing, so we can test for new grounded event
        bool previouslyGrounded = IsGrounded;
        // offset the ray slightly from the floor
        Vector3 startLocation = new Vector3(transform.position.x, 
            transform.position.y + .1f, transform.position.z);
        Debug.DrawRay(startLocation, Vector3.down * .2f);
        if(Physics.Raycast(startLocation, Vector3.down, .2f + .1f, _groundTestLayers))
        {
            IsGrounded = true;
            // check if we JUST touched the ground
            CheckIfLanded();
        }
        else
        {
            // not grounded yet
            IsGrounded = false;
        }
    }

    private void CheckIfLanded()
    {
        // if we were falling, but have recently grounded, we have landed!
        if (IsFalling && IsGrounded)
        {
            Landed.Invoke();
            // reset our jump!
            _doubleJumpReady = true;
        }
    }
    #endregion
}

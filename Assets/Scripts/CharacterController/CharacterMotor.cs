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

    Vector2 _movement = Vector2.zero;
    Vector3 _newMovementThisFrame = Vector3.zero;
    Quaternion _rotationThisFrame;

    Vector3 _knockBack = Vector3.zero;

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
            KnockBack(transform.forward, 2000);
        }
    }

    private void FixedUpdate()
    {
        // calculate
        CheckIfGrounded();
        CheckIfFalling();
        CalculateMomentum();
        // apply
        ApplyJump(_jumpThisFrame);
        ApplyMovement(_newMovementThisFrame);
        ApplyRotation(_rotationThisFrame);
        ApplyKnockBack(_knockBack);
    }
    #endregion

    #region Public
    public void Move(Vector3 moveDirection)
    {
        //Debug.Log("Move Direction: " + moveDirection);
        _newMovementThisFrame = moveDirection;
    }

    public void Rotate(Vector3 moveDirection)
    {       
        _rotationThisFrame = Quaternion.LookRotation(moveDirection);
    }

    public void Jump()
    {
        _jumpThisFrame = true;
    }

    public void KnockBack(Vector3 direction, float strength)
    {
        _knockBack = direction * strength;
    }
    #endregion


    private void CalculateMomentum()
    {
        // if we're currently trying to move, accelerate
        if (_newMovementThisFrame != Vector3.zero)
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
    }

    void ApplyMovement(Vector3 newMovement)
    {
        // if we don't have a movement request, apply momentum
        // in forward direction instead
        if (newMovement == Vector3.zero)
        {
            newMovement = transform.forward;
        }

        // direction * current speed (calculated previously)
        newMovement = newMovement * CurrentSpeed;
        // retain gravity from rigidbody, so we can still fall/jump
        newMovement.y = _rb.velocity.y;
        // FINALLY apply movement
        _rb.velocity = newMovement;

        // clear out our movement
        _newMovementThisFrame = Vector3.zero;
        //TODO actually call this when something moved, not every fixedUpdate
        MovementChanged.Invoke(_movement);
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

    void ApplyKnockBack(Vector3 knockBack)
    {
        // if there's knockback, apply it
        if (_knockBack != Vector3.zero)
        {
            _rb.AddForce(knockBack);
            KnockedBack.Invoke();
            // clear it out for the next frame
            _knockBack = Vector3.zero;
        }
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
}

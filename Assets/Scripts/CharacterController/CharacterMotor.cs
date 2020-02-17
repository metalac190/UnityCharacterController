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
    [SerializeField] float _maxSpeed = .1f;
    public float MaxSpeed
    {
        get => _maxSpeed;
        private set => _maxSpeed = value;
    }
    [SerializeField] float _accelToMaxInSec = 1f;
    [SerializeField] float _decelToZeroInSec = 1f;

    [Header("Rotation")]
    [SerializeField] float _turnSpeed = .1f;

    [Header("Jumping")]
    [SerializeField] float _jumpForce = 100;
    [SerializeField] float _groundCheckRadius = .15f;
    [SerializeField] LayerMask _groundLayers;

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
    public float CurrentSpeedNormalized
    {
        get => (1 / MaxSpeed) * _currentSpeed;
    }
    public bool IsFalling { get; private set; } = true;    // falling is defined as 'after the apex of the jump'
    public bool IsGrounded { get; private set; } = true;

    public float AccelRatePerSecond
    {
        get { return _maxSpeed / _accelToMaxInSec; }
    }
    public float DecelRatePerSecond
    {
        get { return _maxSpeed / _decelToZeroInSec; }
    }

    Rigidbody _rb;

    Vector2 _movement;
    Vector3 _newMovementThisFrame = Vector3.zero;
    Quaternion _rotationThisFrame;

    #region MonoBehaviour
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        CheckIfFalling();
        CheckIfGrounded();
        // Check if we just landed!
        
        ApplyAcceleration();

        if(_newMovementThisFrame != Vector3.zero)
        {
            ApplyMovement(_newMovementThisFrame);
            //TODO add to Vector
        }
        else
        {
            ApplyMovement(transform.forward);
            //TODO subtract from Vector
        }

        ApplyRotation(_rotationThisFrame);

        _newMovementThisFrame = Vector3.zero;

        //TODO actually call this when something moved, not every fixedUpdate
        MovementChanged.Invoke(_movement);
    }
    #endregion

    #region Public
    public void Move(Vector3 moveDirection)
    {
        _newMovementThisFrame = moveDirection;
    }

    public void Rotate(Vector3 moveDirection)
    {       
        _rotationThisFrame = Quaternion.LookRotation(moveDirection);
    }

    public void Jump()
    {
        if (IsGrounded)
        {
            _rb.AddForce(Vector3.up * _jumpForce);
            Jumped.Invoke();
        }
    }

    public void KnockBack(Vector3 direction, float strength)
    {
        _rb.AddForce(direction * strength);
        KnockedBack.Invoke();
    }
    #endregion


    private void ApplyAcceleration()
    {
        if (_newMovementThisFrame != Vector3.zero)
        {
            CurrentSpeed += AccelRatePerSecond * Time.fixedDeltaTime;
        }
        else
        {
            CurrentSpeed -= DecelRatePerSecond * Time.fixedDeltaTime;
        }
        // don't go over the max speed
        CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0, _maxSpeed);
    }

    void ApplyMovement(Vector3 movementThisFrame)
    {
        movementThisFrame *= CurrentSpeed;
        _rb.MovePosition(_rb.position + movementThisFrame);
    }

    void ApplyRotation(Quaternion rotationThisFrame)
    {
        rotationThisFrame.Normalize();
        Quaternion newRotation = Quaternion.Slerp(_rb.rotation, rotationThisFrame, _turnSpeed);
        _rb.MoveRotation(newRotation);
        // don't reset it, so that we maintain current facing direction
    }

    void CheckIfFalling()
    {
        bool previouslyFalling = IsFalling;
        // if we have downward velocity and we're not grounded, then we're falling
        if(_rb.velocity.y < 0 && !IsGrounded)
        {
            IsFalling = true;
            // if we weren't falling before, but now we are, start falling state
            if (previouslyFalling == false)
            {
                StartedFalling.Invoke();
            }
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

        Collider[] groundColliders = Physics.OverlapSphere(transform.position, _groundCheckRadius, _groundLayers);
        if(groundColliders.Length == 0)
        {
            //Debug.Log("Grounded! Collider: " + hitInfo.collider.gameObject.name);
            IsGrounded = false;
        }
        else
        {
            // we are touching ground
            IsGrounded = true;
            // if we were falling, but have recently grounded, we have landed!
            if(IsFalling)
            {
                Landed.Invoke();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.position, _groundCheckRadius);
    }
}

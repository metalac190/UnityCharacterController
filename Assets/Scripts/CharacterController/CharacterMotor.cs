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
    [SerializeField] float _maxSpeed = .1f;
    [SerializeField] float _accelZeroToMax = 1f;
    [SerializeField] float _decelMaxToZero = 1f;

    [SerializeField] float _turnSpeed = .1f;

    [SerializeField] float _jumpForce = 100;

    [SerializeField] float _groundCheckRadius = .15f;
    [SerializeField] LayerMask _groundLayers;

    public event Action<float> SpeedChange = delegate { };
    public event Action Jumped = delegate { };
    public event Action Landed = delegate { };
    public event Action StartedFalling = delegate { };
    public event Action KnockedBack = delegate { };

    public float CurrentSpeed { get; private set; } = 0;    // speed accounting for acceleration
    public bool IsFalling { get; private set; } = true;    // falling is defined as 'after the apex of the jump'
    public bool IsGrounded { get; private set; } = true;

    public float AccelRatePerSecond
    {
        get { return _maxSpeed / _accelZeroToMax; }
    }
    public float DecelRatePerSecond
    {
        get { return _maxSpeed / _decelMaxToZero; }
    }

    Rigidbody _rb;

    Vector3 _currentVelocity;
    Vector3 _moveDirectionThisFrame = Vector3.zero;
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

        if(_moveDirectionThisFrame != Vector3.zero)
        {
            ApplyMovement(_moveDirectionThisFrame);
        }
        else
        {
            ApplyMovement(transform.forward);
        }

        ApplyRotation(_rotationThisFrame);

        _moveDirectionThisFrame = Vector3.zero;
    }
    #endregion

    #region public
    public void Move(Vector3 moveDirection)
    {
        _moveDirectionThisFrame = moveDirection;
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
        if (_moveDirectionThisFrame != Vector3.zero)
        {
            CurrentSpeed += AccelRatePerSecond * Time.fixedDeltaTime;
        }
        else
        {
            CurrentSpeed -= DecelRatePerSecond * Time.fixedDeltaTime;
        }
        // don't go over the max speed
        CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0, _maxSpeed);

        Debug.Log("CurrentSpeed: " + CurrentSpeed);
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

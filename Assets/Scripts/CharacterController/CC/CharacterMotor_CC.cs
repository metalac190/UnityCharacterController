using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor_CC : MonoBehaviour
{
    public event Action JumpStart = delegate { };
    public event Action DoubleJumpStart = delegate { };
    public event Action FallStart = delegate { };
    public event Action Landed = delegate { };

    [Header("Movement")]
    [SerializeField] float _maxSpeed = 5f;
    [SerializeField] float _turnSpeed = 7f;
    
    [Header("Jumping")]
    [SerializeField] float _gravityStrength = -9.81f;
    [SerializeField] float _maxJumpHeight = 3;
    [SerializeField] float _minJumpStrength = 3f;
    [SerializeField] float _continuousJumpStrength = .2f;
    [SerializeField] float _jumpingInputDuration = .15f;
    [SerializeField] Vector3 _groundBoxDimensions = new Vector3(1, .2f, 1);
    [SerializeField] Vector3 _groundOffset;
    [SerializeField] LayerMask _groundedLayers = -1;  // default to 'everything'

    [Header("Double Jump")]
    [SerializeField] bool _allowDoubleJump = true;
    [SerializeField] float _doubleJumpHeight = 2;

    bool _requestJump = false;  // for starting a new jump
    bool _requestJumpContinuous = false;    // for continuous jump force

    bool _isGrounded = false;
    bool _startingJumpSequence = false;
    bool _isFalling = false;
    bool _doubleJumpReady = true;

    Coroutine _groundedCheckLock;

    CharacterController _controller = null;

    Vector3 _requestedMovementThisFrame;
    Quaternion _requestedRotationThisFrame;

    Vector3 _jumpForce = Vector3.zero;
    Vector3 _gravityForce = Vector3.zero;
    Vector3 _groundStickForce = Vector3.zero;
    Vector3 _velocity = Vector3.zero;

    void Reset()
    {
        // disable player layer for default setup. 
        int _playerLayer = 8;
        _groundedLayers = ~(1 << _playerLayer);
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // create new movement vectors, do calculations, apply movement
        // once at the end of this Update frame
        Vector3 newMovement = _requestedMovementThisFrame;
        Quaternion newRotation = _requestedRotationThisFrame;

        //TODO new features still need to add
        // apply move acceleration
        // moving platform

        // controller.isGrounded is unpredictable. Use our own
        // only check for ground if we're not in the middle of our
        // jumping sequence
        if(_startingJumpSequence == false)
            _isGrounded = TestGrounded();
        // check if we have landed. Do this between our ground and falling checks
        CheckIfLanded();

        _isFalling = TestFalling();

        ApplyGravity();
        ApplyGroundStick();
        ApplyJump();

        // apply our velocity to our movement this frame
        newMovement += _jumpForce;
        newMovement += _gravityForce;
        newMovement += _groundStickForce;

        //Debug.Log("New Movement: " + newMovement);

        ApplyMovement(newMovement);
        ApplyRotation(_requestedRotationThisFrame);

        // clear out requests until next frame
        _requestedMovementThisFrame = Vector3.zero;
        _requestJump = false;
        _requestJumpContinuous = false;
    }

    private void ApplyGravity()
    {
        // if we're grounded we don't need to process gravity
        if (_isGrounded)
        {
            //Debug.Log("Apply downward force to hug the ground");
            _gravityForce.y = 0;
        }
        // otherwise we need to keep falling faster
        else
        {
            //Debug.Log("Decrease gravity");
            _gravityForce.y += _gravityStrength * Time.deltaTime;
        }
    }

    void ApplyGroundStick()
    {
        // if we're grounded, apply a force so that we can stick to down slopes
        if (_isGrounded)
        {
            _groundStickForce.y = _gravityStrength;
        }
        else
        {
            _groundStickForce.y = 0;
        }
    }

    private void ApplyJump()
    {
        
        // if jump requirements are valid
        if (_isGrounded && _requestJump)
        {
            Debug.Log("Jump!");
            // start a timer sequence to lock out grounding checks temporarily, 
            // while we get off the ground
            if (_groundedCheckLock != null)
                StopCoroutine(_groundedCheckLock);
            _groundedCheckLock = StartCoroutine
                (GroundedCheckLockoutRoutine(_jumpingInputDuration));
            _isGrounded = false;
            // gravity jump Physics equation -> New upward velocity
            //_velocity.y = Mathf.Sqrt(_maxJumpHeight * -2 * _gravity);
            _jumpForce.y += _minJumpStrength;

            JumpStart.Invoke();
        }
        // if we're in our jump sequence, continue detecting jump requests
        else if (_startingJumpSequence && _requestJumpContinuous)
        {
            
            _jumpForce.y += _continuousJumpStrength;
        }
        // if we're unable to jump
        else if(_requestJump && _allowDoubleJump && _doubleJumpReady)
        {
            // if our jump is expended and we request a new jump,
            // see if we have our double jump available
            Debug.Log("Double Jump!");
            _doubleJumpReady = false;
            // kill previous upwards momentum before applying new jump
            _gravityForce.y = 0;
            // add a flat doubleJump force
            _jumpForce.y = Mathf.Sqrt(_doubleJumpHeight * -2 * _gravityStrength);

            DoubleJumpStart.Invoke();
        }
        // if we're grounded, null out jump force
        else if(_isGrounded)
        {
            _jumpForce.y = 0;
        }
        // ensure we never exceed our max jump height
        float maxJumpStrength = Mathf.Sqrt(_maxJumpHeight * -2 * _gravityStrength);
        _jumpForce.y = Mathf.Clamp(_jumpForce.y, 0, maxJumpStrength);
    }

    // request movement
    public void RequestMove(Vector3 motion)
    {
        _requestedMovementThisFrame = motion * _maxSpeed;
    }

    // request turn
    public void RequestTurn(Vector3 moveDirection)
    {
        // convert Vector3 to a Quaternion
        _requestedRotationThisFrame = Quaternion.LookRotation(moveDirection);
        // ensure that our length is still 1
        _requestedRotationThisFrame.Normalize();
        // rotate towards our new direction, over time
        _requestedRotationThisFrame = Quaternion.Slerp
            (transform.rotation, _requestedRotationThisFrame, 
            _turnSpeed * Time.deltaTime);
    }

    public void RequestJump()
    {
        _requestJump = true;
    }

    public void RequestJumpContinuous()
    {
        _requestJumpContinuous = true;
    }

    void ApplyMovement(Vector3 movement)
    {
        _controller.Move(movement * Time.deltaTime);
    }

    void ApplyRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    bool TestGrounded()
    {
        // if we've found a valid ground collider, we're grounded!
        if(Physics.CheckBox(transform.position + _groundOffset,
            _groundBoxDimensions / 2, Quaternion.identity, _groundedLayers))
        {
            return true;
        }
        // otherwise, don't assume we're grounded
        else
        {
            return false;
        }
    }

    bool TestFalling()
    {
        bool previouslyFalling = _isFalling;
        // if our gravity has overtaken our jump velocity, we're now falling
        float fallSpeed = _gravityForce.y + _jumpForce.y;
        // if we have downward velocity and we're not grounded, then we're falling!
        if(fallSpeed < 0 && !_isGrounded)
        {
            // currently falling
            // if we weren't falling before, but now we are, we STARTED falling
            if(previouslyFalling == false)
            {
                Debug.Log("Start Falling");
                FallStart.Invoke();
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    void CheckIfLanded()
    {
        if(_isGrounded && _isFalling)
        {
            Debug.Log("Landed!");
            Landed.Invoke();
            // reset our double jump
            _doubleJumpReady = true;
        }
    }

    // use this to temporarily suspend grounding checks
    private IEnumerator GroundedCheckLockoutRoutine(float lockoutTime)
    {
        _startingJumpSequence = true;
        yield return new WaitForSeconds(lockoutTime);
        _startingJumpSequence = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position 
            + _groundOffset, _groundBoxDimensions / 2);
    }
}

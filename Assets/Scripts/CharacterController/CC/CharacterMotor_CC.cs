using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor_CC : MonoBehaviour
{
    // event hookups
    public event Action<float> SpeedChanged = delegate { };
    public event Action StoppedMoving = delegate { };
    public event Action StartedMoving = delegate { };
    public event Action JumpStarted = delegate { };
    public event Action DoubleJumpStarted = delegate { };
    public event Action FallStarted = delegate { };
    public event Action Landed = delegate { };

    [Header("[ Movement ]")]
    [SerializeField] float _maxSpeed = 5f;
    [SerializeField] float _turnSpeed = 7f;
    [SerializeField] float _accelToMaxInSec = .5f;  // seconds to hit max speed
    [SerializeField] float _decelToMaxInSec = .3f;  // seconds to decay speed to 0
    [SerializeField] float _airControlMultiplier = 1f;     // how much accel to allow in midair

    [Header("[ Jumping ]")]
    [SerializeField] float _gravityStrength = -9.81f;
    [SerializeField] float _maxJumpHeight = 3;
    [SerializeField] float _minJumpStrength = 2.5f;
    [SerializeField] float _continuousJumpStrength = .2f;
    [SerializeField] float _jumpingInputDuration = .2f;

    [Header("[ Double Jump ]")]
    [SerializeField] bool _isDoubleJumpAllowed = true;
    [SerializeField] float _doubleJumpHeight = 1.5f;

    [Header ("[ Ground Detection ]")]
    [SerializeField] Vector3 _groundBoxDimensions = new Vector3(1, .1f, 1);
    [SerializeField] Vector3 _groundOffset = new Vector3(0,0,0);
    [SerializeField] LayerMask _groundedLayers = -1;  // default to 'everything'

    [Header("[ Miscellaneous ]")]
    [SerializeField] float _groundStickStrength = 1;    // strength that player sticks to ground on slopes

    public bool IsGrounded { get; private set; } = false;
    public bool IsFalling { get; private set; } = false;

    float _currentSpeed = 0;
    public float CurrentSpeed
    {
        get => _currentSpeed;
        private set
        {
            // ensure we don't EVER exceed our max speed
            value = Mathf.Clamp(value, 0, _maxSpeed);
            // check if our speed has changed
            if (value != _currentSpeed)
            {
                SpeedChanged?.Invoke(value);
            }
            // assign it
            _currentSpeed = value;
        }
    }
    public float CurrentMomentumRatio
    {
        get => (1 / _maxSpeed) * CurrentSpeed;
    }
    public float AccelRatePerSecond
    {
        get { return (_maxSpeed / _accelToMaxInSec) * Time.deltaTime; }
    }
    public float DecelRatePerSecond
    {
        get { return (_maxSpeed / _decelToMaxInSec) * Time.deltaTime; }
    }

    bool _isRequestingJump = false;  // for starting a new jump
    bool _isRequestingJumpContinuous = false;    // for continuously requesting added jump force
    bool _isStartingJumpSequence = false; // for defining the 'start period' of a new jump
    bool _isDoubleJumpReady = true;   // whether or not we still have a jump stored

    bool _isMoving = false;

    Coroutine _groundedCheckLock;

    CharacterController _collider = null;

    Vector3 _requestedMovementThisFrame = Vector3.zero;
    Quaternion _requestedRotationThisFrame;
    Vector3 _previousMoveInput = Vector3.zero;

    Vector3 _jumpForce = Vector3.zero;
    Vector3 _gravityForce = Vector3.zero;
    Vector3 _groundStickForce = Vector3.zero;

    #region Monobehaviour
    void Reset()
    {
        // disable player layer for default setup. 
        int _playerLayer = 8;
        _groundedLayers = ~(1 << _playerLayer);
    }

    private void Awake()
    {
        _collider = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // create new movement vectors, calculate move requests, apply movement, clear requests

        //TODO new features still need to add
        // moving platform

        _isMoving = TestIfMoving();
        // only check for ground if we're not in the middle of our
        // jumping sequence. prevents isGrounded = true when leaving the ground
        if (_isStartingJumpSequence == false)
        {
            // controller.isGrounded is unpredictable. Use our own
            IsGrounded = TestGrounded();
        }
        // check if we have landed between our ground and falling checks
        TestIfLanded();
        // we may want to know if we're falling, for animations and gravity reset
        IsFalling = TestFalling();

        CalculateGravity();
        CalculateGroundStick();
        ProcessJumpRequests();

        // combine all of our forces into a single vector
        Vector3 newMovement = _requestedMovementThisFrame;
        Quaternion newRotation = _requestedRotationThisFrame;
        newMovement = ApplyAccelDecel(newMovement);
        newMovement = ApplyForceBuildup(newMovement);
        //Debug.Log("New Movement: " + newMovement);

        // finally, move our player
        ApplyMovement(newMovement);
        ApplyRotation(newRotation);

        
        // store values for checking next frame
        StorePreviousRequests();
        ClearRequests();
    }

    #endregion

    #region Public Requests
    // Other functions can request movement, but it does not guarantee movement

    // request movement
    public void RequestMove(Vector3 motion)
    {
        _requestedMovementThisFrame = motion;
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
        _isRequestingJump = true;
    }

    public void RequestJumpContinuous()
    {
        _isRequestingJumpContinuous = true;
    }
    #endregion

    #region Request Management
    private bool TestIfMoving()
    {
        // if we have some kind of input, we're trying to move
        if (_requestedMovementThisFrame != Vector3.zero)
        {
            // if we weren't moving before, but have new input, we just started moving!
            if (!_isMoving)
            {
                Debug.Log("Started Moving");
                StartedMoving?.Invoke();
            }
            return true;
        }
        // if we don't have input this frame, we're not actively trying to move
        else
        {
            // if we were moving before, but now don't have input, we just stopped
            if (_isMoving)
            {
                Debug.Log("Stopped Moving");
                StoppedMoving?.Invoke();
            }
            return false;
        }
    }

    private void StorePreviousRequests()
    {
        if (_requestedMovementThisFrame != Vector3.zero)
        {
            _previousMoveInput = _requestedMovementThisFrame;
        }
    }

    private void ClearRequests()
    {
        _requestedMovementThisFrame = Vector3.zero;
        _isRequestingJump = false;
        _isRequestingJumpContinuous = false;
    }
    #endregion

    #region Force Calculations
    Vector3 ApplyAccelDecel(Vector3 newMovement)
    {
        // if we're in the air and moving, allow instant velocity change
        if (!IsGrounded && _requestedMovementThisFrame != Vector3.zero)
        {
            //Debug.Log("Air / Accel");
            CurrentSpeed += AccelRatePerSecond * _airControlMultiplier;
            newMovement *= CurrentSpeed;
        }
        // if we're in the air and not moving, kill forward speed
        else if(!IsGrounded && _requestedMovementThisFrame == Vector3.zero)
        {
            //Debug.Log("Air / Decel");
            CurrentSpeed -= DecelRatePerSecond * _airControlMultiplier;
            newMovement = _previousMoveInput * CurrentSpeed;
        }
        // if we're grounded and currently trying to move, accelerate
        else if (IsGrounded && _requestedMovementThisFrame != Vector3.zero)
        {
            //Debug.Log("Ground / Accel");
            CurrentSpeed += AccelRatePerSecond;
            newMovement *= CurrentSpeed;
        }
        // if we're grounded and no move input, decelerate
        else if(IsGrounded && _requestedMovementThisFrame == Vector3.zero)
        {
            //Debug.Log("Ground / Decel");
            CurrentSpeed -= DecelRatePerSecond;
            // if there's no input, decelerate in previous input direction
            newMovement = _previousMoveInput * CurrentSpeed;
        }

        //Debug.Log("Ratio: " + CurrentMomentumRatio);
        //Debug.Log("Current speed: " + _currentSpeed);
        return newMovement;
    }

    // take in a force, apply changes, return force
    private Vector3 ApplyForceBuildup(Vector3 newMovement)
    {
        // accumulate forces and return the combined vector
        newMovement += _jumpForce;
        newMovement += _gravityForce;
        newMovement += _groundStickForce;

        return newMovement;
    }

    private void CalculateGravity()
    {
        // if we're grounded we don't need to process gravity
        if (IsGrounded)
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

    void CalculateGroundStick()
    {
        // if we're grounded, apply a force so that we can stick to down slopes
        if (IsGrounded)
        {
            _groundStickForce.y = _gravityStrength * _groundStickStrength;
        }
        else
        {
            _groundStickForce.y = 0;
        }
    }


    #endregion

    #region Jumping
    private void ProcessJumpRequests()
    {
        // if jump requirements are valid
        if (IsGrounded && _isRequestingJump)
        {
            StartNewJump();
        }
        // if we're in our jump sequence, and holding the jump button
        // continue applying jump height
        else if (_isStartingJumpSequence && _isRequestingJumpContinuous)
        {
            _jumpForce.y += _continuousJumpStrength;
        }
        // if we're unable to jump, but able to double jump, do that instead
        else if (_isRequestingJump && _isDoubleJumpAllowed && _isDoubleJumpReady)
        {
            StartDoubleJump();
        }
        // if we're grounded, null out jump force
        else if (IsGrounded)
        {
            _jumpForce.y = 0;
        }
        // ensure we never exceed our max jump height
        float maxJumpStrength = Mathf.Sqrt(_maxJumpHeight * -2 * _gravityStrength);
        _jumpForce.y = Mathf.Clamp(_jumpForce.y, 0, maxJumpStrength);
    }

    private void StartNewJump()
    {
        Debug.Log("Jump!");
        // start a timer sequence to lock out grounding checks temporarily, 
        // while we get off the ground
        if (_groundedCheckLock != null)
            StopCoroutine(_groundedCheckLock);
        _groundedCheckLock = StartCoroutine
            (GroundedCheckLockoutRoutine(_jumpingInputDuration));

        IsGrounded = false;
        // gravity jump Physics equation -> New upward velocity
        //_velocity.y = Mathf.Sqrt(_maxJumpHeight * -2 * _gravity);
        _jumpForce.y += _minJumpStrength;

        JumpStarted.Invoke();
    }

    private void StartDoubleJump()
    {
        Debug.Log("Double Jump!");
        // if our jump is expended and a new jump is valid,
        // use our doublejump instead
        _isDoubleJumpReady = false;
        // kill previous gravity buildup before applying new jump
        _gravityForce.y = 0;
        // add a flat doubleJump force. Seems weird, but it's convention
        _jumpForce.y = Mathf.Sqrt(_doubleJumpHeight * -2 * _gravityStrength);

        DoubleJumpStarted.Invoke();
    }
    // use this to temporarily suspend grounding checks
    private IEnumerator GroundedCheckLockoutRoutine(float lockoutTime)
    {
        _isStartingJumpSequence = true;
        yield return new WaitForSeconds(lockoutTime);
        _isStartingJumpSequence = false;
    }
    #endregion

    #region Apply Movement
    void ApplyMovement(Vector3 movement)
    {
        _collider.Move(movement * Time.deltaTime);
    }

    void ApplyRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }
    #endregion

    #region Tests
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
        bool previouslyFalling = IsFalling;
        // if our gravity has overtaken our jump velocity, we're now falling
        float fallSpeed = _gravityForce.y + _jumpForce.y;
        // if we have downward velocity and we're not grounded, then we're falling!
        if(fallSpeed < 0 && !IsGrounded)
        {
            // currently falling
            // if we weren't falling before, but now we are, we STARTED falling
            if(previouslyFalling == false)
            {
                Debug.Log("Start Falling");
                FallStarted.Invoke();
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    void TestIfLanded()
    {
        if(IsGrounded && IsFalling)
        {
            Debug.Log("Landed!");
            Landed.Invoke();
            // reset our double jump
            _isDoubleJumpReady = true;
        }
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position 
            + _groundOffset, _groundBoxDimensions / 2);
    }
    #endregion
}

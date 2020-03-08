using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor_CC : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float _maxSpeed = 5f;
    [SerializeField] float _gravity = -9.81f;
    
    [Header("Jumping")]
    [SerializeField] float _jumpHeight = 3;
    [SerializeField] float _groundDistance = .15f;
    [SerializeField] Vector3 _groundOffset;
    [SerializeField] LayerMask _groundedLayers = -1;  // default to 'everything'

    bool _isGrounded = false;

    CharacterController _controller = null;

    Vector3 _velocity;
    Vector3 _newVelocity;

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
        // apply move velocity
        // apply gravity
        // apply jumping
        // moving platform
        // apply downwards force so that we attach to downwards slopes


        // controller.isGrounded is unpredictable. Use our own
        _isGrounded = TestGrounded();

        // apply gravity
        _velocity.y += _gravity * Time.deltaTime;
        // apply movement
        _controller.Move(_velocity * Time.deltaTime);
    }

    public void Move(Vector3 motion)
    {
        //_controller.Move(motion * _maxSpeed * Time.deltaTime);
        _velocity += motion * _maxSpeed;
    }

    public void Jump()
    {
        if (_isGrounded)
        {
            // gravity jump Physics equation
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2 * _gravity);
        }
    }

    bool TestGrounded()
    {
        return Physics.CheckSphere(transform.position + _groundOffset,
            _groundDistance, _groundedLayers);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position 
            + _groundOffset, _groundDistance);
    }
}

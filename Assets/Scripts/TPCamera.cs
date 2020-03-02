using UnityEngine;

public class TPCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] Transform _target = null;
    [SerializeField] Vector3 _lookAtOffset = new Vector3(0, 1, 0);
    [SerializeField] float _currentDistance = 8;
    
    [Header("Speed Modifiers")]
    [SerializeField] float _horizontalSpeed = 40.0f;
    [SerializeField] float _verticalSpeed = 40.0f;
    [SerializeField] float _zoomSpeed = 5;

    [Header("Clamping")]
    [SerializeField] float _yMinLimit = -15f;
    [SerializeField] float _yMaxLimit = 65f;
    [SerializeField] float _distanceMin = 3;
    [SerializeField] float _distanceMax = 15f;

    const float RotateModifier = 0.02f;

    float _currentX = 0.0f;
    float _currentY = 0.0f;

    void Start()
    {
        // get our start rotations
        Vector3 angles = transform.eulerAngles;
        _currentX = angles.y;
        _currentY = angles.x;
    }

    void LateUpdate()
    {
        if (_target)
        {
            if (Input.GetKey(KeyCode.Mouse1))
            {
                // get Input
                _currentX += Input.GetAxis("Mouse X") * _horizontalSpeed * _currentDistance * RotateModifier;
                _currentY -= Input.GetAxis("Mouse Y") * _verticalSpeed * RotateModifier;
            }

            _currentY = ClampAngle(_currentY, _yMinLimit, _yMaxLimit);

            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);

            _currentDistance = Mathf.Clamp(_currentDistance - Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed, _distanceMin, _distanceMax);
            
            //TODO test if raycast has hit the target

            Vector3 cameraOffset = new Vector3(0.0f, 0.0f, -_currentDistance);
            Vector3 _lookAtTarget = _target.position + _lookAtOffset;
            Vector3 position = rotation * cameraOffset + _lookAtTarget;

            // move our camera
            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}

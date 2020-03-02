using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputHelper
{
    public static Vector3 ConvertDirectionToCameraLocal(Vector3 movement, Camera camera)
    {
        Vector3 horizontalMovement = camera.transform.right * movement.x;
        Vector3 forwardMovement = camera.transform.forward * movement.z;
        // get a combined direction with normalized length
        Vector3 moveDirection = (horizontalMovement + forwardMovement).normalized;

        return moveDirection;
    }
}

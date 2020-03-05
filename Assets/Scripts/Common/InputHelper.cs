using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputHelper
{
    public static Vector3 ConvertDirectionToCameraLocal(Vector3 movement, Transform otherTransform)
    {
        Vector3 horizontalMovement = otherTransform.right * movement.x;
        Vector3 forwardMovement = otherTransform.forward * movement.z;
        // get a combined direction with normalized length
        Vector3 moveDirection = (horizontalMovement + forwardMovement).normalized;

        return moveDirection;
    }
}

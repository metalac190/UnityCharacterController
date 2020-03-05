using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pawns are world objects that can be possessed by a player or AI
/// </summary>
public abstract class Pawn : MonoBehaviour
{
    public abstract void OnControlled(PlayerInput player, Camera camera);
    public abstract void OnReleased();
    // references while Controlled
    protected PlayerController Controller;

    // if controlled, hook into input
    public virtual void Control(PlayerController controller, PlayerInput input, Camera camera)
    {
        Controller = controller;

        // send it to the derived class
        OnControlled(input, camera);
    }

    // if released, forget input.
    public virtual void Release()
    {
        OnReleased();
        // clean up
        Controller = null;
    }

    // if this gameObject is disabled, release it justin case
    void OnDisable()
    {
        if (Controller != null)
        {
            Release();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pawns are world objects that can be possessed by a player or AI
/// </summary>
public abstract class Pawn : MonoBehaviour
{
    public abstract void OnControlled();
    public abstract void OnReleased();
    // references while Controlled
    protected PlayerController Controller;
    protected PlayerInput Input;
    protected Camera Camera;

    // if controlled, hook into input
    public virtual void Control(PlayerController controller, PlayerInput input, Camera camera)
    {
        Controller = controller;
        Input = input;
        Camera = camera;

        // send it to the derived class
        OnControlled();
    }

    // if released, forget input.
    public virtual void Release()
    {
        OnReleased();
        // clean up
        Controller = null;
        Input = null;
        Camera = null;
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

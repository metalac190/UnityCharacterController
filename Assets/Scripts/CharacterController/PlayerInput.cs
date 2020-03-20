using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerInput : MonoBehaviour
{
    public event Action<Vector2> MoveInput = delegate { };
    public event Action<Vector2> RotateInput = delegate { };

    public event Action Jump = delegate { };
    public event Action Jumping = delegate { };

    public event Action PrimaryAction = delegate { };
    public event Action SecondaryAction = delegate { };

    public event Action EscapeKey = delegate { };

    void Update()
    {
        DetectMoveInput();
        DetectRotateInput();
        DetectKeys();
    }

    void DetectMoveInput()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");
        if(xInput != 0 || yInput != 0)
        {
            MoveInput?.Invoke(new Vector2(xInput, yInput));
        }
    }

    void DetectRotateInput()
    {

    }

    void DetectKeys()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump?.Invoke();
        }
        if (Input.GetKey(KeyCode.Space))
        {
            Jumping?.Invoke();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscapeKey?.Invoke();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPawn
{
    void OnControlled(PlayerInput input);
    void OnReleased(PlayerInput input);
}

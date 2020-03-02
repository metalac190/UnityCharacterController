using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPawn
{
    void OnControlled(PlayerController controller, PlayerInput input, Camera camera);
    void OnReleased(PlayerController controller, PlayerInput input, Camera camera);
}

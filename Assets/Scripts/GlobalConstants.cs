using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalConstants : MonoBehaviour
{
    public const float GRAVITY_ACCEL_upt = -0.7f; // upt -> Units per Tick (fixed update)

    public const float BOT_COLLISION_THRESH_deg = 45f;
    public const float TOP_COLLISION_THRESH_deg = 45f;
    // anything that is not the BOT or TOP collision is the side
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaffController : MonoBehaviour
{

    public Vector2 collisionLoc; // holds location of latest collison point
    public bool isAttached; // is staff currently in colliding

    void OnCollisionEnter2D(Collision2D other) 
    {
        isAttached = true;
        collisionLoc = transform.position;
    }

    void OnCollisionExit2D(Collision2D other)
    {
        isAttached = false;
    }

}

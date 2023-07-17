using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaffController : MonoBehaviour
{

    public float STAFF_RESISTANCE = 2.0f; // k of Hooke's Law
    public Vector2 collisionLoc; // holds location of latest collison point
    public bool isCollidingStatic; // is staff currently in colliding
    public bool isCollidingKinematic; // is staff currently colliding with a moving object

    void OnCollisionEnter2D(Collision2D other) 
    {
        if (other.gameObject.CompareTag("Static Terrain")) 
        {
            isCollidingStatic = true;
            collisionLoc =  other.collider.ClosestPoint(transform.position);
        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Static Terrain")) 
        {
            isCollidingStatic = false;
        }
    }
}

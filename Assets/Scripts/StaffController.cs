using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaffController : MonoBehaviour
{

    public Vector2 collisionLoc; // holds location of latest collison point
    public bool isColliding; // is staff currently in colliding
    public Rigidbody2D entity;

    void OnCollisionEnter2D(Collision2D other) 
    {
        isColliding = true;
        collisionLoc =  other.collider.ClosestPoint(transform.position);
        // print("in " + collisionLoc);
        print("in");
    }

    void OnCollisionExit2D(Collision2D other)
    {
        isColliding = false;
        print("out");
    }
}

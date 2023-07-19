using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaffController : MonoBehaviour
{

    PlayerController player;
    public float STAFF_RESISTANCE = 2.0f; // k of Hooke's Law
    public Vector2 collisionLoc; // holds location of latest collison point
    public Vector2 collisionDir; // negative of the direction of the staff collision
    public bool isCollidingStatic; // is staff currently in colliding
    public bool isCollidingKinematic; // is staff currently colliding with a moving object

    void Start()
    {
        player = transform.parent.GetComponent<PlayerController>();
    } 

    void OnCollisionEnter2D(Collision2D other) 
    {
        // check whether collision is with static object
        if (other.gameObject.CompareTag("Static Terrain")) 
        {
            // sets location of collision
            isCollidingStatic = true;
            collisionLoc =  other.collider.ClosestPoint(transform.position);
            
            // depending on CheckCollisionDir() outcome, collisionDir is instantiated as one of
            // two orthonormal vectors wrt s_dir. This is used for calculations in 
            // PlayerController.cs's HandleRotationalStaffInput() function to halt moving through walls
            // at any angle 
            if (CheckCollisionDir()) collisionDir = new Vector2(-1*player.s_dir.y, player.s_dir.x);
            else collisionDir = new Vector2(player.s_dir.y, -1*player.s_dir.x);

            // print("in");

        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Static Terrain")) 
        {
            isCollidingStatic = false;
            // print("out");
        }
    }

    // checks which vector perpendicular to the s_dir points toward the collision
    bool CheckCollisionDir() 
    {
        return Physics2D.Raycast(transform.position, new Vector2(player.s_dir.y, -1*player.s_dir.x), .3f);
    }
}

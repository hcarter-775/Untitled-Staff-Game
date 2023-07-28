using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaffController : MonoBehaviour
{

    private PlayerController player; // instance of the PlayerController, to access GetS_Dir()
    private Rigidbody2D staff_rigidbody;
    public Vector2 collisionLoc; // holds location of latest collison point
    public Vector2 collisionDir; // negative of the direction of the staff collision
    public bool isCollidingStatic; // is staff currently in colliding
    public float damping;
    public float resistance; // spring constant
    public float offset;

    void Start()
    {
        player = transform.parent.GetComponent<PlayerController>();
        staff_rigidbody = transform.GetComponent<Rigidbody2D>();
        resistance = 10f;
        damping = 0.35f;
        offset = 0.1f;
    } 

    void OnCollisionEnter2D(Collision2D other) 
    {

        // check whether collision is with static object
        if (other.gameObject.CompareTag("Static Terrain")) 
        {
            isCollidingStatic = true;

            // depending on CheckCollisionDir() outcome, collisionDir is instantiated as one of
            // two orthonormal vectors wrt GetS_Dir(). This is used in player's HandleRotationalStaffInput() 
            // function to halt moving through walls at any angle 
            if (CheckCounterClockwiseSideCollision()) collisionDir = new Vector2(-1*player.GetS_Dir().y, player.GetS_Dir().x);
            else collisionDir = new Vector2(player.GetS_Dir().y, -1*player.GetS_Dir().x);
            
            collisionLoc = other.collider.ClosestPoint(transform.position) - (offset * collisionDir);
            staff_rigidbody.MovePosition(collisionLoc);
        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Static Terrain")) 
        {
            isCollidingStatic = false;
        }
    }

    // checks which vector perpendicular to the s_dir points toward the collision
    public bool CheckCounterClockwiseSideCollision() 
    {
        return Physics2D.Raycast(transform.position, new Vector2(-1*player.GetS_Dir().y, player.GetS_Dir().x), .25f, 7);
    }

    public bool CheckClockwiseSideCollision() 
    {
        return Physics2D.Raycast(transform.position, new Vector2(player.GetS_Dir().y, -1*player.GetS_Dir().x), .25f, 7);
    }

    public bool CheckForwardCollision()
    {
        return Physics2D.Raycast(transform.position, new Vector2(-1*player.GetS_Dir().x, -1*player.GetS_Dir().y), .25f, 7);
    }
    
}

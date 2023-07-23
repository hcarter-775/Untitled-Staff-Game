using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaffController : MonoBehaviour
{
    public ContactFilter2D TipContactFilter;

    // true only while waiting to notify the Player object that there is
    // a new contact in the staff tip
    private bool m_ContactStarting = false;
    private bool m_CurrentlyContacting = false;
    private Vector2 m_ContactLocation;
    private Rigidbody2D m_CollidingBody = null; // this may be null even if there is a contact, this means there is a collision with an object with no Rigidbody

    // copy of the parent player
    private PlayerController m_Player;

    // Start is called before the first frame update
    void Start()
    {
        m_Player = transform.parent.GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        // keep the collision point of the staff pointing away from the player
        // TODO maybe
    }

    // called at the start of a collision
    void OnCollisionEnter2D(Collision2D other) 
    {
        m_ContactStarting = true;
        m_CurrentlyContacting = true;
        m_ContactLocation = other.collider.ClosestPoint(transform.position);
        m_CollidingBody = other.otherRigidbody;
    }

    // called all the time during the collision
    void OnCollisionStay2D(Collision2D other)
    {
        // make sure the contact point is updated correctly in case the contact
        // point changes through a rotation
        m_ContactLocation = other.collider.ClosestPoint(transform.position);
    }

    // called at the end of a collision
    void OnCollisionExit2D(Collision2D other)
    {
        m_CurrentlyContacting = false;
    }

    // see if there is a pending contact event. Will also clear the m_ContactStarting
    // flag
    public bool GetContactStarting()
    {
        bool retval = m_ContactStarting;
        m_ContactStarting = false;
        return retval;
    }

    public bool GetCurrentlyContacting()
    {
        return m_CurrentlyContacting;
    }

    public Vector2 GetContactLocation()
    {
        return m_ContactLocation;
    }

    public Rigidbody2D GetCollidingBody()
    {
        return m_CollidingBody;
    }
}

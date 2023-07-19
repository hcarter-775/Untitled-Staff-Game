using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// enum for the jumping states
public enum JumpState
{
    PENDING_PRESS = 0,
    JUMP_REQUESTED = 1,
    JUMPING = 2,
    PENDING_RELEASE = 3
}

[System.Serializable]
public struct MovementConstants
{
    public Vector2 MaxSpeed;
    public Vector2 Friction; // (accel from friction) = (Friction) * (Speed)**2
    public float XAccelSpeedingUp; // X Accel when increasing speed
    public float XAccelStopping; // X Accel when changing direction speed
    public float XAccelNoInput; // X Accel when the requested movement is 0
    public Vector2 TooFastAccel; // Amount of accel to slow down the player when they are faster than the maxspeed
}

public class PlayerController : MonoBehaviour
{
    /*
    * movement based constants that can be adjusted in the editor
    */

    // basic movement constants
    public MovementConstants GroundedMovement;
    public MovementConstants AirbornMovement;

    // jump / gravity modification constants
    public int NumberOfJumps = 3; // number of FixedUpdates in a row that are jumps. Releasing the space bar will end this
    public float JumpImpulse = 5f; // amount of YSpeed that is given with a jump during each FixedUpdate

    /*
    * END MOVEMENT CONSTANTS
    */

    // what degree difference between cursor position corresponds to a maximum rotational
    // input for moving the staff
    public float DegreesForMaxRotation = 10f;

    // ContactFilters for top, bot, and sides of the player. They will get handled
    // in unique ways in the FixedUpdate section
    public ContactFilter2D BotContactFilter;
    public ContactFilter2D TopContactFilter;
    public ContactFilter2D LeftContactFilter;
    public ContactFilter2D RightContactFilter;

    // updated in the Frame Update() for maximum speed, but should only be handled
    // in the FixedUptade() function for consistancy
    private JumpState m_CurrJumpState;
    private int m_JumpsRemaining;
    private float m_XMoveRequested; // [-1, 1] for how much to move the player
    private float m_RotMoveRequested; // [-1, 1] for how much to rotate the staff

    private Rigidbody2D m_Rigidbody;
    private Vector2 m_Speed;
    private Vector2 totalAccel;

    // We can check to see if there are any contacts given our contact filter
    // which can be set to a specific layer and normal angle.
    // there are two variables from each, the state of one is saved each FixedUpdate
    private bool m_TouchingBot => m_Rigidbody.IsTouching(BotContactFilter);
    private bool m_TouchingTop => m_Rigidbody.IsTouching(TopContactFilter);
    private bool m_TouchingLeft => m_Rigidbody.IsTouching(LeftContactFilter);
    private bool m_TouchingRight => m_Rigidbody.IsTouching(RightContactFilter);
    private bool TBot;
    private bool TTop;
    private bool TLeft;
    private bool TRight;

    /* 

    Harrison's variable additions

    */

    // staff components
    private Transform s_TipLocation; // location of the staff's non-player tip
    private Rigidbody2D s_RigidBody; // rigidBody2d of the staff tip
    private StaffController s_PublicVariables; // staff controller variables, in StaffController.cs
    public Vector2 s_dir; // normed vector, describes staff direction wrt player
    private Vector2 s_currPE = Vector2.zero; // stores current PE in staff
    private float s_rigidLength; // length of uncompressed staff
    private float s_currLength; // current length of staff

    void Start()
    {
        // components setup
        m_Rigidbody = GetComponent<Rigidbody2D>();
        s_TipLocation = transform.GetChild(0).GetComponent<Transform>();
        s_RigidBody = transform.GetChild(0).GetComponent<Rigidbody2D>();
        s_PublicVariables = transform.GetChild(0).GetComponent<StaffController>();

        // staff setup
        s_rigidLength = (transform.position - s_TipLocation.position).magnitude;
        s_dir = (transform.position - s_TipLocation.position).normalized;
        s_currLength = s_rigidLength;

        // setup default movement
        GroundedMovement.MaxSpeed = new Vector2(6f, 20f);
        GroundedMovement.Friction = new Vector2(0.01f, 0.01f);
        GroundedMovement.XAccelSpeedingUp = 0.5f;
        GroundedMovement.XAccelStopping = 2f;
        GroundedMovement.XAccelNoInput = 0.15f;
        GroundedMovement.TooFastAccel = new Vector2(1f, 1f);

        AirbornMovement.MaxSpeed = new Vector2(20f, 30f);
        AirbornMovement.Friction = new Vector2(0.001f, 0.001f);
        AirbornMovement.XAccelSpeedingUp = 0.15f;
        AirbornMovement.XAccelStopping = 1f;
        AirbornMovement.XAccelNoInput = 0.05f;
        AirbornMovement.TooFastAccel = new Vector2(1f, 1f);

        // setup the ContactFilters
        BotContactFilter.useNormalAngle = true;
        BotContactFilter.minNormalAngle = 90 - GlobalConstants.BOT_COLLISION_THRESH_deg;
        BotContactFilter.maxNormalAngle = 90 + GlobalConstants.BOT_COLLISION_THRESH_deg;

        TopContactFilter.useNormalAngle = true;
        TopContactFilter.minNormalAngle = 270 - GlobalConstants.TOP_COLLISION_THRESH_deg;
        TopContactFilter.maxNormalAngle = 270 + GlobalConstants.TOP_COLLISION_THRESH_deg;

        LeftContactFilter.useNormalAngle = true;
        LeftContactFilter.useOutsideNormalAngle = true;
        LeftContactFilter.minNormalAngle = 90 - GlobalConstants.BOT_COLLISION_THRESH_deg;
        LeftContactFilter.maxNormalAngle = 270 + GlobalConstants.TOP_COLLISION_THRESH_deg;

        RightContactFilter.useNormalAngle = true;
        RightContactFilter.minNormalAngle = 90 + GlobalConstants.TOP_COLLISION_THRESH_deg;
        RightContactFilter.maxNormalAngle = 270 - GlobalConstants.BOT_COLLISION_THRESH_deg;
    }

    void Update()
    {
        // Get the desired actions from the player through input
        m_XMoveRequested = Input.GetAxisRaw("Horizontal");
        float m_XJump = Input.GetAxisRaw("Jump");
        // m_XMoveRequested = (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
        if ((m_XJump != 0) && (m_CurrJumpState == JumpState.PENDING_PRESS))
        {
            m_CurrJumpState = JumpState.JUMP_REQUESTED;
        }
        if (m_XJump == 0)
        {
            // always end the jump if the jump key is released
            m_CurrJumpState = JumpState.PENDING_PRESS;
        }

        // get the position of the mouse wrt the player in order to find the desired staff rotation input
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float staffTipAngleWRTPlayer = Mathf.Clamp
        (
            Vector2.SignedAngle(mousePos, s_TipLocation.position - transform.position),
            -1*DegreesForMaxRotation, DegreesForMaxRotation
        );
        m_RotMoveRequested = staffTipAngleWRTPlayer / DegreesForMaxRotation;
    }

    void FixedUpdate()
    {
        // get local variables for all of the Touching Bools
        TBot = m_TouchingBot;
        TTop = m_TouchingTop;
        TLeft = m_TouchingLeft;
        TRight = m_TouchingRight;

        // updates staff direction wrt player
        s_dir = (transform.position - s_TipLocation.position).normalized;

        // no accelleration at the start of the frame
        totalAccel.x = 0;
        totalAccel.y = 0;

        // handle the player left and right linear inputs and the jump input
        totalAccel += HandlePlayerStandardInput();

        // handle staff and player rotational inputs, depends on whether staff is connected to something
        totalAccel += HandleRotationalStaffInput();

        // handle gravity and friction acceleration
        totalAccel += HandleFrictionGravity();

        // if the staff is out, calculate the current state of the staff, including
        // how much it is compressed and the reqired accel change from that
        totalAccel += HandleStaffCompression();

        // limit the velocities of the player as needed
        m_Speed += totalAccel;
        m_Speed += LimitPlayerSpeed();

        // use collision detection to prevent the player from phasing through walls
        if (TBot && m_Speed.y < 0) m_Speed.y = 0;
        if (TTop && m_Speed.y > 0) m_Speed.y = 0;
        if (TLeft && m_Speed.x < 0) m_Speed.x = 0;
        if (TRight && m_Speed.x > 0) m_Speed.x = 0;

        // set the new velocity of the player
        m_Rigidbody.velocity = m_Speed; // TODO this is a pass by refrence I believe, which means this could completely control the speed of the player. Unsure if this is an issue, keep an eye out for it

        // move the relative location of the staff tip to where is will be in the world
        // after this tick. If needed, also move the object that is being grabbed
        // TODO

        // DEBUG
        // if (Input.GetKey(KeyCode.UpArrow))
        // {
        //     Debug.Log("Touching Bot: " + TBot);
        //     Debug.Log("YSpeed: " + m_Speed.y);
        // }
    }

    private Vector2 HandlePlayerStandardInput()
    {
        MovementConstants movementConsts;
        Vector2 accel = new Vector2(0f, 0f);

        // handle jump inputs
        if (m_CurrJumpState == JumpState.JUMP_REQUESTED)
        {
            if (TBot)
            {
                // the jump is starting. Change the state to jumping
                m_CurrJumpState = JumpState.JUMPING;
                m_JumpsRemaining = NumberOfJumps;
            }
            else
            {
                // this is a failed jump. Player must release and press again
                m_CurrJumpState = JumpState.PENDING_RELEASE;
            }
        }

        if (m_CurrJumpState == JumpState.JUMPING)
        {
            accel.y += JumpImpulse;
            TBot = false; // this will force airborn movement right from the jump
            m_JumpsRemaining--;

            if (m_JumpsRemaining <= 0)
            {
                m_CurrJumpState = JumpState.PENDING_RELEASE;
            }

            // Debug.Log("Jumping");
        }

        // choose what set of movementConstants to use right now
        if (TBot) movementConsts = GroundedMovement;
        else movementConsts = AirbornMovement;

        float xSpeedInc = m_XMoveRequested * m_Speed.x;
        if (m_XMoveRequested == 0)
        {
            // no input from the user, use the no input accel to slow down the player
            xSpeedInc = movementConsts.XAccelNoInput;
            if (m_Speed.x <= xSpeedInc && m_Speed.x >= -1*xSpeedInc)
            {
                xSpeedInc = -1*m_Speed.x; // make sure the player stops moving at low speed
            }
            else if (xSpeedInc * m_Speed.x > 0)
            {
                // change the direction if needed to make the player always slow down
                xSpeedInc *= -1;
            }
        }
        else if (xSpeedInc > 0) 
        {
            // same direction -> trying to go faster
            xSpeedInc = m_XMoveRequested * movementConsts.XAccelSpeedingUp;
        }
        else
        {
            // different direction -> slowing down
            xSpeedInc = m_XMoveRequested * movementConsts.XAccelStopping;
        }
        accel.x += xSpeedInc;

        return accel;
    }

    private Vector2 HandleRotationalStaffInput()
    {
        // DEBUG right now the code will just put the end of the staff right at where the
        // mouse is pointed with little physics
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        
        // Whether staff can move in direction of player input- may be blocked by an object
        bool can_rotate = false;

        // staff is colliding with an object 
        if (s_PublicVariables.isCollidingStatic)
        {
            // is player moving away from staff- staff should not get stuck
            if ((transform.position - s_TipLocation.position).magnitude > s_currLength) can_rotate = true;
            // small system of linear equations to tell whether the requested direction of input is allowed
            // basically, can two special orthonormal vectors combine with >=0 constants to get the requested direction
            else 
            {
                Vector2 new_position_dir = (((s_currLength * mousePos.normalized) + (Vector2)transform.position) - s_PublicVariables.collisionLoc).normalized;
                float x_constant = (new_position_dir.x - s_dir.x)/s_PublicVariables.collisionDir.x;
                float y_constant = (new_position_dir.y - s_dir.y)/s_PublicVariables.collisionDir.y;
                float comparison_error = -0.2f; // mathematically perfect system of linear equations would use >= 0 below.
                if (x_constant >= comparison_error && y_constant >= comparison_error) can_rotate = true;

            }
        }
        else can_rotate = true;

        // MovePosition() has better collision detection than a straight position change.
        if (can_rotate) s_RigidBody.MovePosition((s_currLength * mousePos.normalized) + (Vector2)transform.position);
    
        // check if we are connected to anything or not and the mass of what is connected.
        // this will change the point of rotation to somewhere in between the player and
        // object or not
        // TODO

        // get the desired angular velocity depending on player input to move the staff
        // around. Make sure to calculate the initial angular velocity around the tip
        // so angular velocity can be correctly limited
        // TODO

        // if the staff is not connected to anything, it should rotate around the player
        // TODO

        // if the staff is connected, rotations will be around the center of mass between
        // the connected object and the player. For infinite mass objects, the center of
        // mass will be right at the tip of the staff
        // TODO

        return new Vector2(0f, 0f);
    }

    private Vector2 HandleStaffCompression()
    {

        Vector2 accel = Vector2.zero;

        // is staff tip colliding
        if (s_PublicVariables.isCollidingStatic) 
        {
            // gets current distance from player to staff tip
            s_currLength = (transform.position - (Vector3)s_PublicVariables.collisionLoc).magnitude;

            // added acceleration : force dir of staff * delta staff diff * hooke's constant             
            accel = s_dir * (s_rigidLength - s_currLength) * s_PublicVariables.STAFF_RESISTANCE;

            // save the total acc given to the staff
            s_currPE += accel;
        } 
        else 
        {
            // increases staff length until at rigid length
            if (s_currLength < s_rigidLength) 
            {
                s_currLength = Mathf.Min((transform.position - (Vector3)s_PublicVariables.collisionLoc).magnitude, s_rigidLength); 
            }

            // releases staff's stored energy in one update
            accel = s_currPE;
            s_currPE = Vector2.zero;
        }
        return accel;
    }

    // will be called in HandleStaffCompression() fxn
    private Vector2 HandleStaffToEntityInteraction() {
        // check whether collision with fixed loc or not.

        // change staff length based on acceleration from non-player side

        // save amount of energy in staff based on this length-change

        // use acceleration of staff itself to define what amount of totalAcc/s_currPE 
        // is moved into the entity being hit (if not a fixed loc)

        // Calvin's notes from HandleStaffCompression(), may be better suited here.

        // get the accelerations of the player and the object that the tip is connected to

        // based on the masses of the player and this collision object, 
        // create fixed object layer, these have infinite mass
        // if not fixed object, measure where it was hit, and what direction it was going, and 
        // with what acceleration
        // TODO 

        return Vector2.zero;
    }

    private Vector2 HandleFrictionGravity()
    {
        MovementConstants movementConsts;
        Vector2 accel = new Vector2(0f, 0f);

        // pick the correct movement constants
        if (TBot) movementConsts = GroundedMovement;
        else movementConsts = AirbornMovement;

        // handle accelerations gravity
        accel.y += GlobalConstants.GRAVITY_ACCEL_upt;

        // handle air resistance
        Vector2 frictionAccel = Vector2.Scale(movementConsts.Friction, 
                                              Vector2.Scale(m_Speed, m_Speed));
        if (m_Speed.x > 0f) frictionAccel.x *= -1;
        if (m_Speed.y > 0f) frictionAccel.y *= -1;
        accel += frictionAccel; // air resistance

        return accel;
    }

    private Vector2 LimitPlayerSpeed()
    {
        MovementConstants movementConsts;
        Vector2 accel = new Vector2(0f, 0f);

        // pick the correct movement constants
        if (TBot) movementConsts = GroundedMovement;
        else movementConsts = AirbornMovement;

        // limit speed of the player. The player can only be slowed down by the TooFastAccel
        // constant for a less jarring stop
        if (m_Speed.x >= movementConsts.MaxSpeed.x)
        {
            accel.x = movementConsts.MaxSpeed.x - m_Speed.x;
            accel.x = Mathf.Max(accel.x, -1*movementConsts.TooFastAccel.x);
        }
        if (m_Speed.x <= -1*movementConsts.MaxSpeed.x)
        {
            accel.x = -1*movementConsts.MaxSpeed.x - m_Speed.x;
            accel.x = Mathf.Min(accel.x, movementConsts.TooFastAccel.x);
        }
        if (m_Speed.y >= movementConsts.MaxSpeed.y)
        {
            accel.y = movementConsts.MaxSpeed.y - m_Speed.y;
            accel.y = Mathf.Max(accel.y, -1*movementConsts.TooFastAccel.y);
        }
        if (m_Speed.y <= -1*movementConsts.MaxSpeed.y)
        {
            accel.y = -1*movementConsts.MaxSpeed.y - m_Speed.y;
            accel.y = Mathf.Min(accel.y, movementConsts.TooFastAccel.y);
        }

        return accel;
    }
}

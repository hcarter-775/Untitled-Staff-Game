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

// enum for staff extension states
public enum staff_state
{
    is_extended = 0,
    is_extending = 1,
    is_descended = 2,
    is_descending = 3
}

// enum for mouse control states
public enum mouse_control_state
{
    on_staff = 0,
    on_player = 1
}

[System.Serializable]
public struct MovementConstants
{
    public Vector2 max_speed;
    public Vector2 Friction; // (accel from friction) = (Friction) * (Speed)**2
    public float XAccelSpeedingUp; // X Accel when increasing speed
    public float XAccelStopping; // X Accel when changing direction speed
    public float XAccelNoInput; // X Accel when the requested movement is 0
    public Vector2 TooFastAccel; // Amount of accel to slow down the player when they are faster than the max_speed
}

[System.Serializable]
public struct RotationConstants
{
    public float max_speed;
    public float base_speed;
    public float accel_speedingUp;
    public float accel_changingDir;
    public float accel_noInput;
    public float radians;
    public int   signed;
}

public class PlayerController : MonoBehaviour
{
    /*
    * start movement constants : can be changed in editor
    */

    // basic movement constants
    public MovementConstants GroundedMovement;
    public MovementConstants AirbornMovement;

    public RotationConstants staff_rotation;
    public RotationConstants player_rotation;

    // jump and gravity modification constants
    public int NumberOfJumps = 3; // number of FixedUpdates in a row that are jumps. Releasing the space bar will end this
    public float JumpImpulse = 5f; // amount of YSpeed that is given with a jump during each FixedUpdate

    /*
    * end movement constants :
    *
    * start contact filters :
    */

    // for top, bot, and sides of the player. handled uniquely in FicedUpdate()
    public ContactFilter2D BotContactFilter;
    public ContactFilter2D TopContactFilter;
    public ContactFilter2D LeftContactFilter;
    public ContactFilter2D RightContactFilter;

    // We can check to see if there are any contacts given our contact filter
    // which can be set to a specific layer and normal angle.
    // there are two variables from each, the state of one is saved in FixedUpdate()
    private bool m_TouchingBot => p_rb.IsTouching(BotContactFilter);
    private bool m_TouchingTop => p_rb.IsTouching(TopContactFilter);
    private bool m_TouchingLeft => p_rb.IsTouching(LeftContactFilter);
    private bool m_TouchingRight => p_rb.IsTouching(RightContactFilter);
    private bool TBot;
    private bool TTop;
    private bool TLeft;
    private bool TRight;

    /* 
    * end contact filters
    *
    * start update variables :
    */

    // updated in Update() for maximum speed, but should only
    // be handled in the FixedUpdate() function for consistancy
    private mouse_control_state current_mouse_state;
    public staff_state current_staff_state;
    private JumpState m_CurrJumpState;
    private Vector2 mouse_wc;
    private int m_JumpsRemaining;
    private float m_XMoveRequested;   // [-1, 1] for how much to move the player
    private float m_RotDistRequested; // the delta theta change from current position to current mouse position 
    private float m_RotSignRequested; // -1, 0 or 1
    private bool m_StaffExtensionRequest; // 1 or -1. 1 is out, -1 is in


    /*
    * end update variables
    * 
    * start player and staff components :
    */

    private float rot_velocity;

    // player components 
    private Rigidbody2D p_rb;
    private Vector2 p_velocity;
    private Vector2 total_accel;
    private Vector2 total_player_control_accel;
    private Vector2 total_staff_control_accel;
    private Vector2 m_prevPos;
    private float p_rad;

    // staff components
    private StaffController staff;
    private Rigidbody2D s_rb;
    private Vector2 s_dir; // normed vector, describes staff direction wrt player
    private float s_LenRigid; // length of uncompressed staff
    private float s_LenCurr; // current length of staff
    public float s_extend = 0.15f;
    private float s_rad;

    /*
    * end player and staff components
    * 
    */

    void Start()
    {
        // components setup
        p_rb = GetComponent<Rigidbody2D>();
        s_rb = transform.GetChild(0).GetComponent<Rigidbody2D>();
        staff = transform.GetChild(0).GetComponent<StaffController>();

        // staff setup
        s_LenRigid = (p_rb.position - s_rb.position).magnitude;
        s_LenCurr = s_LenRigid;
        
        Physics2D.IgnoreCollision(transform.GetComponent<CapsuleCollider2D>(), 
        transform.GetChild(0).GetComponent<CircleCollider2D>());
        
        // setup default rotation
        staff_rotation.max_speed = 14f;
        staff_rotation.base_speed = 9f;
        staff_rotation.accel_speedingUp = 0.4f;
        staff_rotation.accel_noInput = 0.8f;
        staff_rotation.accel_changingDir = 0.5f;
        staff_rotation.signed = -1;
        // staff_rotation.radians = Mathf.Acos(s_dir.x);
        s_rad = Mathf.Acos(s_dir.x);

        player_rotation.max_speed = 12f;
        player_rotation.base_speed = 8f;
        player_rotation.accel_speedingUp = 0.4f;
        player_rotation.accel_noInput = 0.8f;
        player_rotation.accel_changingDir = 0.5f;
        player_rotation.signed = 1;  
        // player_rotation.radians = -1*Mathf.Acos(s_dir.x);      
        p_rad = -1*Mathf.Acos(s_dir.x);

        /*  above are my things, and below were previous additions */

        // setup default movement
        GroundedMovement.max_speed = new Vector2(6f, 20f);
        GroundedMovement.Friction = new Vector2(0.01f, 0.01f);
        GroundedMovement.XAccelSpeedingUp = 0.5f;
        GroundedMovement.XAccelStopping = 2f;
        GroundedMovement.XAccelNoInput = 0.15f;
        GroundedMovement.TooFastAccel = new Vector2(1f, 1f);

        AirbornMovement.max_speed = new Vector2(20f, 30f);
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

    /* 
    * Start Update Control Functions 
    */
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

        // left click the mouse to extend/descend the staff
        if (Input.GetMouseButtonDown(0) && !m_StaffExtensionRequest)
            m_StaffExtensionRequest = true;

        // Get desired staff rotation (in radians) from player input
        mouse_wc = (Vector2) Camera.main.ScreenToWorldPoint(Input.mousePosition) - p_rb.position;
        float signedAngle = Vector2.SignedAngle(mouse_wc, -s_dir) * Mathf.Deg2Rad;
        m_RotDistRequested = Mathf.Abs(signedAngle);
        m_RotSignRequested = (int) Mathf.Sign(signedAngle);
    }

    void FixedUpdate()
    {
        // get local variables for all of the Touching Bools
        TBot = m_TouchingBot;
        TTop = m_TouchingTop;
        TLeft = m_TouchingLeft;
        TRight = m_TouchingRight;

        // no accelleration at the start of the frame
        total_accel.x = 0;
        total_accel.y = 0;

        // handles player movement, friction, and gravity
        total_accel += HandlePlayerControlFlow();

        // handles all staff components, including player rotation, staff extension, and compression
        total_accel += HandleStaffControlFlow();

        // limit the velocities of the player as needed
        p_velocity += total_accel;
        p_velocity += LimitPlayerSpeed();

        // use collision detection to prevent the player from phasing through walls
        if (TBot && p_velocity.y < 0) p_velocity.y = 0;
        if (TTop && p_velocity.y > 0) p_velocity.y = 0;
        if (TLeft && p_velocity.x < 0) p_velocity.x = 0;
        if (TRight && p_velocity.x > 0) p_velocity.x = 0;

        // set the new velocity of the player
        p_rb.velocity = p_velocity; // TODO this is a pass by refrence I believe, which means this could completely control the speed of the player. Unsure if this is an issue, keep an eye out for it
        
        // m_prevPos = p_rb.position;
        // move the relative location of the staff tip to where is will be in the world
        // after this tick. If needed, also move the object that is being grabbed
        // TODO
    }

    private Vector2 HandlePlayerControlFlow()
    {
        // no acceleration at the start of frame
        total_player_control_accel = Vector2.zero;

        // handle the player left and right linear inputs and the jump input
        total_player_control_accel += HandlePlayerStandardInput();

        // handle gravity and friction acceleration
        total_player_control_accel += HandleFrictionGravity();

        return total_player_control_accel;
    }

    private Vector2 HandleStaffControlFlow()
    {
        total_staff_control_accel = Vector2.zero;

        // handle staff extension input
        HandleStaffExtensionInput();

        if (current_staff_state != staff_state.is_descended)
        {
            // updates staff direction wrt player
            s_dir = (p_rb.position - s_rb.position).normalized;

            // if the staff is out, calculate the current state of the staff, including
            // how much it is compressed and the reqired accel change from that            
            if (current_staff_state == staff_state.is_extended) 
            {
                Vector2 curr_player_velocity = p_velocity + total_player_control_accel;
                total_staff_control_accel += HandleStaffCompression(curr_player_velocity);
            }
            
            // handle staff and player rotational inputs, depends on whether staff is connected to something
            if (current_staff_state != staff_state.is_extending)
            {
                HandleRotationalStaffInput();
            }
        }
        else
        { 
            // keep staff centered on player if unextended
            s_rb.MovePosition(p_rb.position);
        }

        return total_staff_control_accel;
    }

    /* 
    * End Update Control Functions 
    * 
    * Start Player Controller Functions (includes physics)
    */

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
                                              Vector2.Scale(p_velocity, p_velocity));
        if (p_velocity.x > 0f) frictionAccel.x *= -1;
        if (p_velocity.y > 0f) frictionAccel.y *= -1;
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
        if (p_velocity.x >= movementConsts.max_speed.x)
        {
            accel.x = movementConsts.max_speed.x - p_velocity.x;
            accel.x = Mathf.Max(accel.x, -1*movementConsts.TooFastAccel.x);
        }
        if (p_velocity.x <= -1*movementConsts.max_speed.x)
        {
            accel.x = -1*movementConsts.max_speed.x - p_velocity.x;
            accel.x = Mathf.Min(accel.x, movementConsts.TooFastAccel.x);
        }
        if (p_velocity.y >= movementConsts.max_speed.y)
        {
            accel.y = movementConsts.max_speed.y - p_velocity.y;
            accel.y = Mathf.Max(accel.y, -1*movementConsts.TooFastAccel.y);
        }
        if (p_velocity.y <= -1*movementConsts.max_speed.y)
        {
            accel.y = -1*movementConsts.max_speed.y - p_velocity.y;
            accel.y = Mathf.Min(accel.y, movementConsts.TooFastAccel.y);
        }

        return accel;
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
        }

        // choose what set of movementConstants to use right now
        if (TBot) movementConsts = GroundedMovement;
        else movementConsts = AirbornMovement;

        float xSpeedInc = m_XMoveRequested * p_velocity.x;
        if (m_XMoveRequested == 0)
        {
            // no input from the user, use the no input accel to slow down the player
            xSpeedInc = movementConsts.XAccelNoInput;
            if (p_velocity.x <= xSpeedInc && p_velocity.x >= -1*xSpeedInc)
            {
                xSpeedInc = -1*p_velocity.x; // make sure the player stops moving at low speed
            }
            else if (xSpeedInc * p_velocity.x > 0)
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

    /*
    * End Player Controller Functions (includes physics)
    * 
    * Start Staff Controller Functions (includes physics)
    */

    private Vector2 HandleStaffCompression(Vector2 linear_player_velocity)
    {
        Vector2 accel = Vector2.zero;

        // is staff tip colliding
        if (staff.isCollidingStatic) 
        {
            // get what player position would have been using: p = p + vt
            Vector2 linear_player_position = p_rb.position + (linear_player_velocity * Time.fixedDeltaTime);

            // gets current distance from player to staff tip
            s_LenCurr = Mathf.Min((linear_player_position - staff.collisionLoc).magnitude, s_LenRigid);

            // added acceleration : force dir of staff * delta staff diff * spring constant
            // added dampening : -1 * current velocity * damping constant            
            accel += (s_dir * (s_LenRigid - s_LenCurr) * staff.resistance);
            accel += -1 * p_rb.velocity * staff.damping;
        }
        else if (s_LenCurr < s_LenRigid) 
        {
            // finds new relative staff position 
            // first, with v = v + at, and then with p = p - vt.
            staff.relative_velocity = staff.relative_velocity + (staff.resistance * Time.fixedDeltaTime);

            Vector2 relative_staff_position = s_rb.position - (s_dir * staff.relative_velocity * Time.fixedDeltaTime);

            // gets current distance from player to staff tip, stops at s_LenRigid, resets relative velocity
            s_LenCurr = (p_rb.position - relative_staff_position).magnitude;
            if (s_LenCurr > s_LenRigid) 
            { 
                staff.relative_velocity = 0f; 
                s_LenCurr = s_LenRigid; 
            }
            
            // added acceleration : force dir of staff * delta staff diff * spring constant             
            accel += (s_dir * (s_LenRigid - s_LenCurr) * staff.resistance);
        }

        return accel;
    }

    private Vector2 HandleStaffToEntityInteraction() 
    {
        
        // will be called in HandleStaffCompression() fxn

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

    /*
    * End Staff Controller Functions (includes physics)
    *
    * Start Staff Controller Functions (not including physics - includes radians)
    */

    private void HandleRotationalStaffInput()
    {
        Vector2 accel = Vector2.zero;
        bool mouse_should_control_player = false;
        
        if (staff.isCollidingStatic) 
        {
            Vector2 mouse_wrt_staff = mouse_wc + p_rb.position - staff.collisionLoc;
            float dot = Vector2.Dot(mouse_wrt_staff, staff.collisionDir);
            if (dot >= 0) mouse_should_control_player = true;
        }
        
        // find the new rotator's radian position wrt the new origin
        if ((current_mouse_state == mouse_control_state.on_staff) && mouse_should_control_player)
        {
            SetCurrentRadians(true);
            current_mouse_state = mouse_control_state.on_player;
        }
        else if ((current_mouse_state == mouse_control_state.on_player) && TBot && !mouse_should_control_player)
        {
            SetCurrentRadians(false);
            current_mouse_state = mouse_control_state.on_staff;
        }
  
        // player or staff movement conditionals
        if (staff.isCollidingStatic && (mouse_should_control_player || !TBot))
        {
            RotateAround(p_rb, s_rb, player_rotation, true);
        }
        else 
        {
            RotateAround(s_rb, p_rb, staff_rotation, false);
        }


        // debugging
        if (Input.GetKey(KeyCode.E))
        {
            // print ("stafftheta: " + s_rad);
            print(s_dir);
            // print ("playertheta: " + player_rotation.radians);
            // print("rot req: " + m_RotSignRequested + " " + m_RotDistRequested);
            // print("rotsignrequest: " + m_RotSignRequested);
            // print("mscp"  + mouse_should_control_player);
            // print(" ");
        }
    
        /*
        bool player_is_moving_away_from_staff = true;

        if (staff.isCollidingStatic) // check things on collision
        {                            
            Vector2 check = p_rb.position - m_prevPos;
            float dot = Vector2.Dot(check, staff.collisionDir);
            if (dot <= 0) player_is_moving_away_from_staff = false;
        }
        
        // staff should be on the ground 
        else if (mouse_should_control_player && (s_LenCurr < s_LenRigid))
        {
            // these two are very similar. when pimafs : staff needs to grow to full size before it can rotate
            // when not, staff has to be at full length before moving. rotating amounts to straightening it out
            // if (player_is_moving_away_from_staff) 
            // {
            if (s_LenCurr + 0.05 > s_LenRigid) 
            {
                float diff = s_LenRigid - s_LenCurr;
                s_LenCurr = s_LenRigid;
                if (Mathf.Abs(s_dir.x) >= Mathf.Abs(s_dir.y))
                {
                    p_rb.position = new Vector2(p_rb.position.x + diff, p_rb.position.y);
                }
                else
                {
                    p_rb.position = new Vector2(p_rb.position.x, p_rb.position.y + diff);
                }
            }
            // }
        }
        else if (player_is_moving_away_from_staff && !mouse_should_control_player && (s_LenCurr >= s_LenRigid))
        {
            // staff should be moved by player change amount (from last update to this frame? is this one frame off? )
            s_rb.position += p_rb.position - m_prevPos;
        }

        // // todo: possibly do nothing on this input 
        // // these two are very similar. when pimafs : staff needs to grow to full size before it can rotate
        // // when not, staff has to be at full length before moving. rotating amounts to straightening it out
        // else if (mouse_should_control_player && (s_LenCurr < s_LenRigid))
        // {
        //     // in case where pimafs: do nothing.
        //     // in other case, rotating amounts to straightening it out (maybe straightens faster?)
        // }
        */
        
        /*
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
        */

    }

    private float RotateAround(Rigidbody2D rotated, Rigidbody2D around, RotationConstants rot, bool is_player)
    {
        Vector2 tangent;
        if (TLeft && is_player && (m_RotSignRequested == 1)) tangent = s_dir;
        else if (TRight && is_player && (m_RotSignRequested == -1)) tangent = s_dir;

        else if (m_RotDistRequested > 0)
        {
            rot_velocity = Mathf.Min(rot_velocity + rot.accel_speedingUp, rot.max_speed);
            float rotation_amount = (rot_velocity/100) * m_RotDistRequested;

            if (is_player)
            {
                if (m_RotSignRequested > 0) p_rad -= rotation_amount;
                else p_rad += rotation_amount;
                tangent = new Vector2(Mathf.Cos(p_rad), Mathf.Sin(p_rad));
                around.MovePosition(staff.collisionLoc);
            }
            else
            {
                if (m_RotSignRequested > 0) s_rad -= rotation_amount;
                else s_rad += rotation_amount;
                tangent = new Vector2(Mathf.Cos(s_rad), Mathf.Sin(s_rad));
            }
        }
        else
        {
            rot_velocity = Mathf.Max(rot_velocity - rot.accel_noInput, rot.base_speed);
            tangent = rot.signed * s_dir;
        }

        rotated.MovePosition((tangent * s_LenCurr) + around.position);

        return 0;
    }

    private void HandleStaffExtensionInput()
    {
        // first
        if (m_StaffExtensionRequest)
        {
            // reset button
            m_StaffExtensionRequest = false;
            
            if (current_staff_state == staff_state.is_extended)
            {
                current_staff_state = staff_state.is_descending;
            }
            else if (current_staff_state == staff_state.is_descended)
            {
                current_staff_state = staff_state.is_extending;
            }
            else if (current_staff_state == staff_state.is_extending)
            {
                current_staff_state = staff_state.is_descending;
            }
            else if (current_staff_state == staff_state.is_descending)
            {
                current_staff_state = staff_state.is_extending;
            }
        }

        // then,
        if (current_staff_state == staff_state.is_extending)
        {
            s_LenCurr = Mathf.Min(s_LenCurr + s_extend, s_LenRigid);
            s_rb.MovePosition((mouse_wc.normalized * s_LenCurr) + p_rb.position);
            
            if (s_LenCurr == s_LenRigid) 
            {
                SetCurrentRadians(false);
                current_staff_state = staff_state.is_extended;
                current_mouse_state = mouse_control_state.on_staff;
            }
            else if (staff.CheckForwardCollision() || staff.CheckClockwiseSideCollision() || staff.CheckCounterClockwiseSideCollision())
            {
                current_staff_state = staff_state.is_descending;
            }
        }
        else if (current_staff_state == staff_state.is_descending)
        {
            s_LenCurr = Mathf.Max(s_LenCurr - s_extend, 0f);
            if (s_LenCurr <= 0f) current_staff_state = staff_state.is_descended;
        }
    }
    
    /*
    * End Staff Controller Functions (not including physics - includes radians)
    * 
    * Start Helper Functions (General)
    */

    private void SetCurrentRadians(bool is_player)
    {
        if (is_player)
        {
            if (s_dir.y > 0) p_rad = Mathf.Acos(s_dir.x);
            else p_rad = -1*Mathf.Acos(s_dir.x);
        }
        else 
        {
            if (-1*s_dir.y > 0) s_rad = Mathf.Acos(-1*s_dir.x);
            else s_rad = -1*Mathf.Acos(-1*s_dir.x);
        }
    }

    public Vector2 GetS_Dir() { return s_dir; }

}
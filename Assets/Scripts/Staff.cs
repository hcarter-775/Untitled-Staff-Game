// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class Staff : MonoBehaviour {
    
//     void Start() {
//         rb = GetComponent<Rigidbody2D>();
//         player = transform.parent.GetComponent<Player>();
//         player_loc = transform.parent.GetComponent<Rigidbody2D>();
//         speed = base_speed;
//     }

//     // take negative inverse of the staff, this line is perpenicular to the staff

//     void Update() {

//         // staff rotation
//         rot_dir = Input.GetAxisRaw("Staff Rotation");
//         if (rot_dir != 0) {

//             isVaulting = true;
//             Vector2 norm_staff_pos = (player_loc.position - rb.position).normalized;
//             float cos = norm_staff_pos.x;
//             float sin = norm_staff_pos.y;
//             // float cos = Mathf.Cos(rb.rotation * Mathf.Deg2Rad);
//             // float sin = Mathf.Sin(rb.rotation * Mathf.Deg2Rad);
            
//             // raise/lower acceleration logic, based on sine and cosine of rotation angle
//             if (((rot_dir < 0) && ((cos >= 0) || ((speed == max_lower_speed) && (sin <= -0.75)))) 
//             ||  ((rot_dir > 0) && ((cos <  0) || ((speed == max_lower_speed) && (sin <= -0.75))))) 
//                 speed = Mathf.Clamp(speed + (lower_acc*Time.deltaTime), base_speed, max_lower_speed);
//             else 
//                 speed = Mathf.Clamp(speed + (raise_acc*Time.deltaTime), base_speed, max_raise_speed); 

//             // movement options based on whether vaulting or not
//             // if (!isAttached) {
//             //     transform.rotation *= Quaternion.Euler(0f, 0f, rot_dir * speed); 
//             // } else {
//             //     transform.RotateAround(staff_contact_point, Vector3.back, rot_dir * speed);
//             //     transform.position = Vector3.MoveTowards(transform.position, player_loc.position, positionCorrection);
//             // }
//             // last_rot_dir = rot_dir;
//             // rb.MoveRotation(rot_dir*speed);
//             // rb.AddTorque(rot_dir*2);
//             // rb.AddTorque(rot_dir*speed);
//             // print(norm_staff_pos);
//             // Vector2 neg_inv_staff = -Inverse(norm_staff_pos);
//             Vector2 perp_norm_staff = Vector2.Perpendicular(norm_staff_pos);
//             // print( Vector2.Dot(perp_norm_staff, norm_staff_pos));
//             // rb.velocity = rot_dir*speed*neg_inv_staff;
//             rb.AddTorque(rot_dir*speed);
//             // rb.velocity += -rot_dir*speed*perp_norm_staff;
//             last_dir = rot_dir;
//         } else {
//             // rb.velocity = Vector2.zero;
//             speed = Mathf.Clamp(speed - (4*Time.deltaTime), 0, 6);
//             // rb.AddTorque(-speed); 
//             rb.AddTorque(-last_dir*speed);
//         }

//         // Vector2 Inverse(Vector2 v) { return new Vector2(1/v.x, 1/v.y); }
        
//         // unattended staff
//         // } else { 
//         //     isVaulting = false;
//         //     if (player.isGrounded()) {
//         //         speed = Mathf.Clamp(speed - (let_go_speed*Time.deltaTime), base_speed, max_lower_speed); 
//         //     } else if (isAttached) {
//         //         // float fall_dir = 0;
//         //         float staff_length = staff_contact_point.x - transform.position.x;
//         //         // if ((staff_length) < 0) fall_dir = 1f; else fall_dir = -1f;
//         //         print(player.GetSpeed());
//         //         print(player.GetSpeed() * Time.deltaTime);
//         //         transform.RotateAround(staff_contact_point, Vector3.back, (player.GetSpeed()/staff_length)*Time.deltaTime);
//         //         // rb.angularVelocity = fall_dir*player.GetSpeed();
//         //         transform.position = Vector3.MoveTowards(transform.position, player_loc.position, positionCorrection);  
//         //     }
//         //     // if (!hasExited) {
//         //     //     transform.rotation *= Quaternion.Euler(0f, 0f, -last_rot_dir * max_lower_speed); 
//         //     //     print("transform.rotation");

//         //     // }
//         // }    
//     }

//     // void OnCollisionEnter2D(Collider2D other) {
//     //     if (other.gameObject.CompareTag("Staff_Contact")) {
//     //         print("Entered");
//     //         isAttached = true;
//     //         hasExited = false;
//     //         staff_contact_point = other.ClosestPoint(staff_tip.position);
//     //     }    
//     // }
//     // void OnTriggerEnter2D(Collider2D other) {
//     //     if (other.CompareTag("Staff_Contact")) {
//     //         print("Entered");
//     //         isAttached = true;
//     //         hasExited = false;
//     //         staff_contact_point = other.ClosestPoint(staff_tip.position);
//     //     }
//     // }

//     // void OnTriggerExit2D(Collider2D other) {
//     //     if (other.CompareTag("Staff_Contact")) {
//     //         print("Exited");
//     //         isAttached = false;
//     //         hasExited = true;
//     //     }
//     // }

//     // public float GetRotDir() { return rot_dir; }
//     // public bool IsVaulting() { return isVaulting; }
//     // public bool IsAttached() { return isAttached; }
//     // public float GetRotation() { return rb.rotation; }

//     // components
//     Rigidbody2D rb;
//     Player player;
//     Rigidbody2D player_loc; // rotation point

//     // acceleration and speed of rotation behavior
//     float rot_dir;
//     float speed;
//     float last_dir;
//     float base_speed = 0.4f;

//     float lower_acc = 2f;
//     float max_lower_speed = 4.5f;

//     float raise_acc = 0.2f;
//     float max_raise_speed = 0.5f;

//     float let_go_speed = 0.8f;

//     // staff vaulting behavior
//     // float positionCorrection = 1.0f;
//     bool isVaulting;
//     bool isAttached;
//     bool hasExited;
//     public Vector2 staff_contact_point;

// }
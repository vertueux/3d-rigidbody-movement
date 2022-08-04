// Copyright (c) Virtuous. Licensed under the MIT license.
// See LICENSE.md in the project root for license information.
// Note that I wrote this script when I was 12 so don't be surprised of some not logical choices.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRules 
{
    public class PlayerMovement : MonoBehaviour 
    {
        [Header("Mouse Looking / Camera Rotation")]
        public Transform playerCamera;  // Find the camera that the local player is using.
        public Transform orientation; // Used to find local orientation of the player.
        [SerializeField] private Vector2 mouseSensitivity = new Vector2(70f, 70f); // The Sensitivity of the Mouse (Which makes the Camera move). 
        [SerializeField] private float maxMouseRotationUp = 90f; // Maximum Upward Rotation of the Camera. 
        [SerializeField] private float maxMouseRotationDown = 90f; // Maximum Downward Rotation of the Camera. 
        private float xLocalRotation = 0f; // Local Player X Rotation 

        [Header("Camera Offset Stuff")]
        [SerializeField] private float offsetMove = 0.5f; // The Distance (Or Move) that the Camera will make.
        [SerializeField] private float offsetSpeed = 1.3f; // The Speed of the offset.
        [SerializeField] private float offsetDuration = 0.5f; // The Duration of the offset.
        private bool moveOffset; // Know if we should play the offset.
        private Vector3 originalPlayerCameraPositon = new Vector3(0f, 0f, 0f); // Find the original position of the Camera.
        private Vector3 offsetVector; // The whole vector of the offset.

        [Header("Movement for Player")]
        public float movementMaxSpeed = 7f; // Maximum Player Speed.
        public float runningMovementMaxSpeed = 10f; // Maximum Player Speed when running.
        private float movementForce; // The Force that will move the Player.
        private float airmovementForce; // The Force that will move the Player in the air.
        [SerializeField] private float counterMovement = 10f; // The value of Counter-Movement, to simulate staticity.
        private float originalmovementForce; // Find the original Force Speed.
        private float originalMovementMaxSpeed; // Find the original Max Speed.

        [Header("Jumping and Gravity")]
        public bool unlimitedSpeed = true; // Air Strafing activated? ;)
        public float jumpForce = 250f; // The Jumping Force.
        public float doubleJumpForce = 250f;
        private bool canDoubleJump = true;
        public float mass = 6f; // The Player's Mass.
        [HideInInspector] public bool readyForJumping = true; // Is the Player ready to jump again?
        [HideInInspector] public bool isJumping; // Does the player jump (or try to jump).

        [Header("Crouching & Sliding")]
        public float crouchMovementMaxSpeed = 5f; // Maximum Player Speed when Crouching.
        private float crouchmovementForce; // The Force that will move the Player when Crouching.
        [SerializeField] private float crouchScale = 0.3f; // The Height (Or Scale)of the player when he is crouching.
        public float crouchScaleSpeed = 6f; // Speed to Crouch.
        private Vector3 crouchVector; // The whole vector of Crouching.
        private Vector3 originalCrouchScale; // The Original Crouch Scale.
        [HideInInspector] public bool isCrouching; // Is the Player's Crouching?

        [Header("Ground Detection ")]
        [SerializeField] private LayerMask groundMask; // The Layer for Ground Detection.
        [HideInInspector] public bool isGrounded = true; // Is the Player's Grounded? / Detect if Colliding with Ground.
        private Vector3 normalVector = Vector3.up;
        private Vector3 normal;
        public float maxSlopeAngle = 35f;


        [Header("Moving Inputs & Camera Inputs")]
        [SerializeField] public KeyCode jump = KeyCode.Space; // Input (Or KeyCode) for Jumping.
        [SerializeField] public KeyCode crouch = KeyCode.CapsLock; // Input (Or KeyCode) for Crouching.
        [SerializeField] public KeyCode run = KeyCode.LeftShift; // Input (Or KeyCode) for Running.
        [SerializeField] public KeyCode dash = KeyCode.A; // Input (Or KeyCode) for using the speed dash.
        [HideInInspector] public float x, y; // Moving Input comming from InputManager.
        [HideInInspector] public float mouseX, mouseY; // Looking Input comming from InputManager.

        [Header("Components Needed for this Script")]
        [HideInInspector] public Rigidbody body; // Physical Body / RigidBody, based on Physics, essential for this Script.
        [HideInInspector] public CapsuleCollider playerCollider; // Collider of Player for Colliding with things...

        [Header("Extras Movement")]
        public float dashForce = 250f;
        public float waitTime = 5; // In seconds.
        private bool canDash = false;
        public float wallDistance = 1f;
        [HideInInspector] public bool isWallRight;
        [HideInInspector] public bool isWallLeft;
        private bool didAWallJump = false;
        //private bool canWallJump = false;
        public float wallForce = 20f;
        public float slideCounterMovement = 3f;
        private bool isSliding = false;
        public float slideForce = 2000f;
        bool timeFinishedAndNotGrounded = false;
        /// <summary> 
        /// On Called (Before Start). 
        /// </summary>
        private void Awake() 
        {
            GetComponents();
            LockMouse();
        }

        /// <summary> 
        /// Listened Every Fixed Frames.
        /// </summary>
        private void FixedUpdate() 
        {
            Movement();
            LimitAirStrafing(195, movementMaxSpeed / 5, movementMaxSpeed / 5, 145);
            WallJump();
            MoreExtrasMovement();
        }

        /// <summary> 
        /// Listened Every Frames.
        /// </summary>
        private void Update() 
        {
            PlayerInput();
            MouseLook();
            CameraOffset();
        }

        /// <summary>
        /// Store the Inputs of the Player.
        /// </summary>
        private void PlayerInput() 
        {
            // Gets the Moving Inputs from the InputManager.
            y = Input.GetAxis("Vertical");
            x = Input.GetAxis("Horizontal");

            // Gets the Mouse Inputs from the InputManager.
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity.x * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity.y * Time.deltaTime;

            // Starting Crouch and Go Up
            if (Input.GetKey(crouch) && isGrounded) StartCrouch();
            if (Input.GetKeyUp(crouch)) GoUp();

            // If the Player isn't Jumping anymore.
            if(Input.GetKeyUp(jump)) isJumping = false;
        }

        /// <summary>
        /// Move the Player / Ground Detection.
        /// </summary>
        private void Movement() 
        {
            // Simulate Mass (Or Extra Gravity).
            body.AddForce(Vector3.down * mass);

            // Counteract Forces, Simulate Stacitity and Max Speed (Only when Grounded).
            CounterMovement();

            // Calculate the Local Velocity of the Player's Physical Body.
            Vector3 localVelocity = transform.InverseTransformDirection(body.velocity);

            //If speed is larger than maxspeed, cancel out the Inputs so you don't go over max speed
            if ( x > 0 && localVelocity.x > movementMaxSpeed ) x = 0;
            if ( x < 0 && localVelocity.x < -movementMaxSpeed ) x = 0;
            if ( y > 0 && localVelocity.z > movementMaxSpeed ) y = 0;
            if ( y < 0 && localVelocity.z < -movementMaxSpeed ) y = 0;

            // If the Player is Pressing the Jump Button, He will Jump.
            if(readyForJumping && Input.GetKey(jump) && isGrounded) Jump();

            // Limiting Starting Air Strafing, by Normalizing.
            if(isJumping && Physics.Raycast(transform.position, Vector3.down, transform.localScale.y + 0.3f, groundMask) && 
                new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && unlimitedSpeed == false) {
                    
                Vector3 normalize = body.velocity.normalized * movementMaxSpeed;
                body.velocity = new Vector3(normalize.x, body.velocity.y, normalize.z);
            }

            if (isSliding && isGrounded && !isJumping) {
                body.AddForce(Vector3.down * Time.deltaTime * slideForce);
                return;
            }

            if (isGrounded && timeFinishedAndNotGrounded)
                canDash = true;

            // Some More Movements. 
            ExtrasMovement();

            // Set the Moving Velocity with y & x moving Input, movement Speed and the DeltaTime.
            var velocity = ((transform.forward * y) + (transform.right * x)).normalized * movementForce * Time.deltaTime;

            // Create Forces to move the Player.
            body.AddForce(velocity);
        }

        /// <summary>
        /// Counteract Forces When the Player is Grounded
        /// </summary>
        private void CounterMovement() 
        {
            if (!isGrounded || isJumping) return; // Don't play this Void if the Player isn't Grounded or Jumping.

            if (isSliding == true) {
                body.AddForce(slideForce * Time.deltaTime * -body.velocity.normalized * slideCounterMovement);
                return;
            }

            // Counteract the Movement (Grounded) || Simulate Fake Friction.
            if(x == 0 && y == 0) {
                Vector3 Counteract = new Vector3(0, body.velocity.y, 0);
                body.velocity = Vector3.Lerp(body.velocity, Counteract, counterMovement * Time.deltaTime);
            }

            //Limit diagonal Moving. / Limit Moving by Normalizing the Speed.
            if (new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed) {
                Vector3 normalize = body.velocity.normalized * (movementMaxSpeed - 1.6f); 
                body.velocity = new Vector3(normalize.x, body.velocity.y, normalize.z);
            }
        }

        /// <summary>
        /// More Movements 
        /// </summary>
        private void ExtrasMovement() 
        {
            // Smooth Crouching.
            if(isCrouching) transform.localScale = Vector3.Lerp(transform.localScale, crouchVector, crouchScaleSpeed * Time.deltaTime);
            if(!isCrouching) transform.localScale = Vector3.Lerp(transform.localScale, originalCrouchScale, crouchScaleSpeed * Time.deltaTime);

            // Air Movement.
            if(!isGrounded) 
            {
                movementForce = airmovementForce;
                if (didAWallJump)
                    canDoubleJump = true;
            }
            else 
            {
                didAWallJump = false;
                canDoubleJump = true;
            }

            if (Input.GetKey(run) && isGrounded && !isCrouching) 
                movementMaxSpeed = runningMovementMaxSpeed;
            else 
                movementMaxSpeed = originalMovementMaxSpeed;

            // Original Movement.
            if(isGrounded && !Input.GetKey(run)) 
            {
                movementForce = originalmovementForce;
                if(!isCrouching) 
                    movementMaxSpeed = originalMovementMaxSpeed;
            }

            // Crouch Max Speed.
            if(isCrouching && isGrounded && body.velocity.magnitude > originalMovementMaxSpeed ) {
                movementMaxSpeed = crouchMovementMaxSpeed;
                isSliding = true;
            } 

            // Double jump.
            if(Input.GetKey(jump) && !isGrounded && !Physics.Raycast(transform.position, -orientation.right, wallDistance * 2.5f) &&
                !Physics.Raycast(transform.position, orientation.right, wallDistance * 2.5f) && canDoubleJump && !isJumping) {
                    body.velocity = new Vector3(body.velocity.x, 0f, body.velocity.z);
                    body.AddForce(transform.up * doubleJumpForce * 1.5f);
                    canDoubleJump = false;
                    didAWallJump = false;
            }
        }

        IEnumerator DashCoolDown() 
        {
            yield return new WaitForSeconds(waitTime);
            isGrounded = false;
            if (isGrounded)
            {
                canDash = true;
                timeFinishedAndNotGrounded = false;
            } 
            else 
                timeFinishedAndNotGrounded = true;
        }

        // Why "MoreExtrasMovement" and not directly in the "ExtrasMovements" function ? Because this one is directly played on Unity's Update() function.
        private void MoreExtrasMovement() 
        {
            if (Input.GetKey(dash) && canDash) 
            {
                isGrounded = false;
                body.velocity = new Vector3(0f, 0f, 0f);
                body.AddForce(playerCamera.transform.forward * dashForce * 1.5f);
                body.AddForce(playerCamera.transform.up * (dashForce / 3));
                StartCoroutine("DashCoolDown");
                canDash = false;
            }
        }

        /// <summary>
        /// Void for The Player's Jump (Add a Force Up).
        /// </summary>
        private void Jump()
        {
            isJumping = true;
            body.AddForce(transform.up * jumpForce * 1.5f);

            // Reset Y velocity (Also don't do Camera Offset).
            if (body.velocity.y < 0.5f)
                body.velocity = new Vector3(body.velocity.x, 0, body.velocity.z);
            else if (body.velocity.y > 0) 
                body.velocity = new Vector3(body.velocity.x, body.velocity.y / 2, body.velocity.z);

            readyForJumping = false;

            // Making a Jump CoolDown to counter High Up Force when Jumping.
            Invoke(nameof(ResetJump), 0.25f);
        }

        /// <summary>
        /// Reset the Player Jumping (For Bunny Hopping).
        /// </summary>
        private void ResetJump() 
        {
            readyForJumping = true;
        }

        /// <summary>
        /// Start Crouching.
        /// </summary>
        private void StartCrouch() 
        {
            isCrouching = true;
        }

        /// <summary>
        /// Stop Crouching.
        /// </summary>
        private void GoUp() 
        {
            transform.position = new Vector3(transform.position.x, transform.position.y + (crouchScale - (crouchScale / 1.1f)), transform.position.z);
            isCrouching = false;
            isSliding = false;
        }

        /// <summary>
        /// Limit a little bit the Air Strafing.
        /// Useful to max the Speed in air.
        /// </summary>
        private void LimitAirStrafing(float counterStrafe, float DetectDiagonalSpeed, float DetectStrafe, float counterDiagonalStrafe) 
        {
            // Calculate a LocalSpace Velocity.
            Vector3 vel = transform.InverseTransformDirection(body.velocity);


            /* Come on! Don't judge I was 12 when I wrote this :/ |    */
            /*                                                    v    */



            // Counter Only when the Player is Moving Forward.
            if(y > 0 && new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && !isGrounded && vel.z > DetectStrafe && x == 0f) 
            {
                body.AddForce(-transform.forward * 1f * counterStrafe * movementMaxSpeed * Time.deltaTime);
            }

            // Counter Only when the Player is Moving Back.
            if(y < 0 && new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && !isGrounded && vel.z < -DetectStrafe && x == 0f) 
            {
                body.AddForce(-transform.forward * -1f * counterStrafe * movementMaxSpeed * Time.deltaTime);
            }

            // Counter Only when the Player is Moving Right.
            if(x > 0 && new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && !isGrounded && vel.x > DetectStrafe && y == 0f) 
            {
                body.AddForce(-transform.right * 1f * counterStrafe * movementMaxSpeed * Time.deltaTime);
            }
            
            // Counter Only when the Player is Moving Left.
            if(x < 0 && new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && !isGrounded && vel.x < -DetectStrafe && y == 0f) 
            {
                body.AddForce(-transform.right * -1f * counterStrafe * movementMaxSpeed * Time.deltaTime);
            }

            // Counter Only when the Player is Moving Forward and Right.
            if(y > 0 && x > 0 && new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && !isGrounded && vel.x > DetectDiagonalSpeed && vel.z > DetectDiagonalSpeed) 
            {
                body.AddForce(-transform.right * 1f * movementMaxSpeed * (counterDiagonalStrafe / 2) * Time.deltaTime);
                body.AddForce(-transform.forward * 1f * movementMaxSpeed * (counterDiagonalStrafe / 2) * Time.deltaTime);
            }

            // Counter Only when the Player is Moving Back and Left.
            if(y < 0 && x < 0 && new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && !isGrounded && vel.x  < -DetectDiagonalSpeed && vel.z < -DetectDiagonalSpeed) 
            {
                body.AddForce(-transform.right * -1f * movementMaxSpeed * (counterDiagonalStrafe / 2) * Time.deltaTime);
                body.AddForce(-transform.forward * -1f * movementMaxSpeed * (counterDiagonalStrafe / 2) * Time.deltaTime);
            }

            // Counter Only when the Player is Moving Back and Right.
            if(y < 0 && x > 0 && new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && !isGrounded && vel.x > DetectDiagonalSpeed && vel.z < -DetectDiagonalSpeed) 
            {
                body.AddForce(-transform.right * 1f * movementMaxSpeed * (counterDiagonalStrafe / 2) * Time.deltaTime);
                body.AddForce(-transform.forward * -1f * movementMaxSpeed * (counterDiagonalStrafe / 2) * Time.deltaTime);
            }

            // Counter Only when the Player is Moving Forward and Left.
            if(y > 0 && x < 0 && new Vector2(body.velocity.x, body.velocity.z).magnitude > movementMaxSpeed && !isGrounded && vel.x < -DetectDiagonalSpeed && vel.z > DetectDiagonalSpeed) 
            {
                body.AddForce(-transform.right * -1f * movementMaxSpeed * (counterDiagonalStrafe / 2) * Time.deltaTime);
                body.AddForce(-transform.forward * 1f * movementMaxSpeed * (counterDiagonalStrafe / 2) * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Mouse Looking.
        /// Make the Player look Around the Scene.
        /// </summary>
        private void MouseLook() 
        {
            // Set Max localRotation & Set the x localRotation with y
            xLocalRotation -= mouseY;
            xLocalRotation = Mathf.Clamp(xLocalRotation, -maxMouseRotationUp, maxMouseRotationDown);

            // Draw a Ray Forward the Player to see what is facing.
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 200f, Color.green);

            // Rotate the Camera
            playerCamera.transform.localRotation = Quaternion.Euler(xLocalRotation, 0f, 0f);

            // Rotate the Body (x)
            transform.Rotate(Vector3.up * mouseX);
        }

        /// <summary>
        /// Lock the Mouse when the Game's Start
        /// </summary>
        private void LockMouse() 
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>
        /// Making an Offset to the Camera when Hitting Ground.
        /// </summary>
        private void CameraOffset() 
        {
            // Offset to the Camera
            if(!isGrounded) moveOffset = true;
            if(isGrounded && moveOffset == true) {
                playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, offsetVector, offsetSpeed * Time.deltaTime);
                StartCoroutine(Offset());
            }

            if(moveOffset == false) playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, originalPlayerCameraPositon, offsetSpeed * Time.deltaTime);
        }

        private void WallJump()
        {
            isWallRight = Physics.Raycast(transform.position, orientation.right, wallDistance);
            isWallLeft = Physics.Raycast(transform.position, -orientation.right, wallDistance);

            Vector3 resetYVelocity = new Vector3(body.velocity.x, 0, body.velocity.z);
            if(!isGrounded && isWallRight && Input.GetKey(jump) && /*canWallJump &&*/ !isJumping)
            {
                //canWallJump = false;
                didAWallJump = true;
                body.velocity = resetYVelocity;
                body.AddForce(transform.up * wallForce * 4, ForceMode.Impulse); 
                body.AddForce(-transform.right * wallForce, ForceMode.Impulse);
            }

            if(!isGrounded && isWallLeft && Input.GetKey(jump) && /*canWallJump &&*/ !isJumping)
            {
                //canWallJump = false;
                didAWallJump = true;
                body.velocity = resetYVelocity;
                body.AddForce(transform.up * wallForce * 4, ForceMode.Impulse); 
                body.AddForce(transform.right * wallForce, ForceMode.Impulse);
            }

            /*if(isGrounded)
            {
                canWallJump = true;
            }*/
        }

        /// <summary>
        /// Offset Coroutine (Offset Time).
        /// </summary>
        private IEnumerator Offset() 
        {
            // Offset Duration.
            yield return new WaitForSeconds(offsetDuration);
            moveOffset = false;
        }

        private bool IsFloor(Vector3 v) 
        {
            float angle = Vector3.Angle(Vector3.up, v);
            return angle < maxSlopeAngle;
        }

        private bool cancellingGrounded;
    
        private void OnCollisionStay(Collision other) 
        {
            // Make sure we are only checking for walkable layers.
            int layer = other.gameObject.layer;
            if (groundMask != (groundMask | (1 << layer))) return;

            // Iterate through every collision in a physics update.
            for (int i = 0; i < other.contactCount; i++) 
            {
                normal = other.contacts[i].normal;
                //FLOOR
                if (IsFloor(normal)) {
                    isGrounded = true;
                    cancellingGrounded = false;
                    normalVector = normal;
                    CancelInvoke(nameof(StopGrounded));
                }
            }

            //Invoke ground/wall cancel, since we can't check normals with CollisionExit
            float delay = 3f;
            if (!cancellingGrounded)
            {
                cancellingGrounded = true;
                Invoke(nameof(StopGrounded), Time.deltaTime * delay);
            }
        }
        private void StopGrounded() {
            isGrounded = false;
        }

        /// <summary>
        /// Get floats, Vectors, Components when the Game is Starting.
        /// </summary>
        private void GetComponents() 
        {
            playerCollider = GetComponent<CapsuleCollider>();
            body = GetComponent<Rigidbody>();

            // Get Original Scale.
            originalCrouchScale = transform.localScale;

            // Set Speed Value.
            movementForce = movementMaxSpeed * 600f;

            // Set Air Movement.
            airmovementForce = movementForce / 6f;

            // Set Crouch Movement.
            crouchmovementForce = movementMaxSpeed * 450f;

            // Get the Original Speed
            originalmovementForce = movementForce;
            originalMovementMaxSpeed = movementMaxSpeed;

            // Get the Vector Scale.
            crouchVector.x = transform.localScale.x;
            crouchVector.y = crouchScale;
            crouchVector.z = transform.localScale.z;

            originalPlayerCameraPositon.x = playerCamera.transform.localPosition.x;
            originalPlayerCameraPositon.y = playerCamera.transform.localPosition.y;
            originalPlayerCameraPositon.z = playerCamera.transform.localPosition.z;

            // Get Offset Vector for Offset the Camera.
            offsetVector.x = playerCamera.transform.localPosition.x;
            offsetVector.y = playerCamera.transform.localPosition.y - offsetMove;
            offsetVector.z = playerCamera.transform.localPosition.z;

            canDash = true;
        }
   }
}
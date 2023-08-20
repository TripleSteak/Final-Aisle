using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Client character controller.
/// </summary>
public sealed class PlayerController : MonoBehaviour
{
    private GameObject _playerObject;
    private SkinnedMeshRenderer _playerRenderer;
    private Animator _playerAnimator;
    private Rigidbody _playerRigidbody;
    private CapsuleCollider _playerCollider;

    [SerializeField]
    private GameObject cameraObject;

    private float _playerScale; // scale of player model in real world
    private bool _isPlayerTransparent; // whether the player model's rendering mode is set to transparent

    public static float CameraMinDist = 0.5f;
    public static float CameraMaxDist = 20f;

    private const float AutoCameraRotationSpeed = 720f; // speed at which camera returns to normal angle, in degrees/second

    private Vector3 _followPoint; // point which is observed by camera
    private Vector2 _followPointVelocity = Vector2.zero; // velocity of horizontal change in follow point position
    private float _followPointVerticalVelocity = 0; // velocity of vertical change in follow point position

    private const float HorizontalPanSpeed = 270f; // speed at which player can horizontally pan camera, in degrees/second
    private const float VerticalPanSpeed = 135f; // speed at which player can vertically pan camera, in degrees/second
    private const float HorizontalTurnSpeed = 135f; // speed at which A and D buttons rotate the player

    private float _verticalAngle; // angle between camera and horizontal plane (90 degrees means looking straight down)
    private float _horizontalAngle; // measure of the camera view angle, NOT the camera's position relative to the player (0 means looking same direction as player)
    private float _playerHorizontalAngle; // direction the player should be facing

    private float _curCameraDist; // how far the camera is from the player, with 0 being directly on the player
    private float _targetCameraDist; // how far the camera SHOULD be from the player, without obstructions
    private float _cameraZoomVelocity; // velocity of camera zoom change, used for smoothing
    private const float ScrollZoomVelocity = 1.0f; // zoom change per mouse scroll "click"

    private float _prevMouseX = 0;
    private float _prevMouseY = 0; // previous mouse coordinates

    private AbsoluteMousePosition _beforeDragPos; // previous mouse coordinates before dragging, in ABSOLUTE full screen coordinates (not app screen)
    private bool _isDraggingMouse;

    private float _nextBlink; // value of Time.time at which the player should blink
    private const float MinBlinkDelay = 0.2f; // minimum number of seconds between consecutive blinks
    private const float MaxBlinkDelay = 1.6f; // maximum number of seconds between consecutive blinks

    private float _lastJumpTime; // value of Time.time at which the player last initiated a jump
    private const float JumpDelay = 0.5f; // minimum time between two consecutive jumps, in seconds
    private bool _isGrounded;

    private float _lastProneTime; // value of Time.time at which the player last toggled prone
    private const float ProneDelay = 0.5f; // minimum time between two consecutive prone toggles, in seconds
    private bool _isProne;

    private const float RollSpeedMultiplier = 1.5f; // speed at which the player rolls, as a percentage of normal speed
    private const float RollDuration = 0.8f; // number of seconds needed for roll to complete
    private float _lastRollTime; // value of Time.time at which last roll was initiated
    private float _rollAngle; // absolute horizontal angle at which player is rolling
    private float _rollAngleVelocity; // keeps track of horizontal angle restoration after roll
    private bool _isRolling;

    private Vector2 _lastMovementInput = new Vector2(0, 0); // last WASD movement input, to determine if new animation needs to start

    private const float ForwardMoveSpeed = 8.5f; // world space coordinates per second
    private const float BackwardMoveSpeed = 4.5f; // world space coordinates per second
    private const float StrafeSpeed = 6f; // world space coordinates per second
    private const float JumpForce = 18f; // world space Newtons
    private const float ProneSpeedMultiplier = 0.5f; // speed at which the player moves while prone, as a percentage of normal speed

    private float _proneAngleXSpeed; // speed at which X prone angle is changing
    private float _proneAngleYSpeed; // speed at which Y prone angle is changing
    private float _proneAngleZSpeed; // speed at which Z prone angle is changing
    private const float ProneAngleSmoothSpeed = 0.2f; // prone angle smooth damp modifier

    /*
     * Player/camera control keys
     */
    public static KeyCode MoveForward = KeyCode.W;
    public static KeyCode MoveBackward = KeyCode.S;
    public static KeyCode StrafeLeft = KeyCode.Q;
    public static KeyCode StrafeRight = KeyCode.E;
    public static KeyCode TurnLeft = KeyCode.A;
    public static KeyCode TurnRight = KeyCode.D;

    public static KeyCode Jump = KeyCode.Space;
    public static KeyCode Roll = KeyCode.V;
    public static KeyCode ToggleCrouch = KeyCode.LeftControl;

    public static KeyCode PanWithTurn = KeyCode.Mouse1;
    public static KeyCode PanNoTurn = KeyCode.Mouse0;

    public void Start()
    {
        _playerObject = gameObject;
        _playerRenderer = _playerObject.transform.GetChild(1).gameObject.GetComponent<SkinnedMeshRenderer>();
        _playerAnimator = GetComponent<Animator>();
        _playerRigidbody = GetComponent<Rigidbody>();
        _playerCollider = GetComponent<CapsuleCollider>();

        _playerScale = _playerObject.transform.localScale.x;
        _playerHorizontalAngle = _playerObject.transform.rotation.y;

        // Set starting camera viewport properties
        _verticalAngle = 30f;
        _horizontalAngle = 0;

        _curCameraDist = 18f;
        _targetCameraDist = 18f;

        Physics.queriesHitBackfaces = true;
    }

    public void FixedUpdate()
    {
        // Check for roll input only if enough time has passed since the last successful roll
        if (Time.time >= _lastRollTime + RollDuration)
        {
            if (_isGrounded && !_isProne && Input.GetKey(Roll))
            {
                var rollInput = _lastMovementInput == Vector2.zero ? new Vector2(0, 1) : _lastMovementInput;

                // Determine new angle to roll at
                _rollAngle = _playerHorizontalAngle;
                
                if (rollInput.x == 0)
                {
                    if (rollInput.y == -1)
                    {
                        _rollAngle += 180;
                    }
                }
                else if (rollInput.x == 1)
                {
                    if (rollInput.y == 1)
                    {
                        _rollAngle += 45;
                    }
                    else if (rollInput.y == 0)
                    {
                        _rollAngle += 90;
                    }
                    else
                    {
                        _rollAngle += 135;
                    }
                }
                else
                {
                    if (rollInput.y == 1)
                    {
                        _rollAngle -= 45;
                    }
                    else if (rollInput.y == 0)
                    {
                        _rollAngle -= 90;
                    }
                    else
                    {
                        _rollAngle -= 135;
                    }
                }

                _playerAnimator.SetTrigger("Roll");
                _isRolling = true;
                _lastRollTime = Time.time;

                _rollAngleVelocity = 0;
            }
            else if (_isRolling)
            { 
                // RollDuration has expired, so we are ending the roll here
                _isRolling = false;
                _lastMovementInput = Vector2.zero;
            }
        }

        // If the player is currently rolling, ensure that the player is moving in the correct rolling direction
        if (_isRolling)
        {
            if (Time.time <= _lastRollTime + 0.12f)
            { 
                // Just a moment after _isRolling is turned to true
                var rotationY = Mathf.SmoothDampAngle(_playerObject.transform.rotation.eulerAngles.y, _rollAngle, ref _rollAngleVelocity, 0.04f);
                _playerObject.transform.rotation = Quaternion.Euler(_playerObject.transform.rotation.x, rotationY, _playerObject.transform.rotation.z);
            }
            else if (Time.time >= _lastRollTime + RollDuration - 0.12f)
            { 
                // Just a moment before _isRolling is turned to false
                var rotationY = Mathf.SmoothDampAngle(_playerObject.transform.rotation.eulerAngles.y, _playerHorizontalAngle, ref _rollAngleVelocity, 0.12f);
                _playerObject.transform.rotation = Quaternion.Euler(_playerObject.transform.rotation.x, rotationY, _playerObject.transform.rotation.z);
            }
            else
            {
                _rollAngleVelocity = 0;
            }

            var moveVelocity = new Vector2(0, ForwardMoveSpeed * RollSpeedMultiplier);
            var rotatedMoveVelocity = Quaternion.Euler(0, _rollAngle, 0) * (new Vector3(moveVelocity.y, _playerRigidbody.velocity.y, -moveVelocity.x));

            // Check if the player can move forward freely
            var velocityRay = new Ray(_playerObject.transform.position + new Vector3(0, 2f, 0), rotatedMoveVelocity.normalized);
            if (Physics.SphereCast(velocityRay, 0.4f, out _, 1.5f))
            { 
                // Cannot move forward due to blockage
                _playerRigidbody.velocity = new Vector3(0, _playerRigidbody.velocity.y, 0);
            }
            else
            {
                _playerRigidbody.velocity = rotatedMoveVelocity;
            }
        }

        // Check for prone toggle input only if enough time has passed since the last prone toggle
        if (Time.time - _lastProneTime >= ProneDelay && _isGrounded && !_isRolling)
        {
            if (Input.GetKey(ToggleCrouch))
            {
                if (!_isProne)
                { 
                    // Enter prone mode
                    _playerAnimator.ResetTrigger("Exit Prone");
                    _playerAnimator.SetTrigger("Enter Prone");
                    _playerCollider.direction = 0;
                    _playerCollider.height = 2f;
                    _playerCollider.center = new Vector3(0.2f, 1, 0);
                    _playerObject.transform.position = _playerObject.transform.position + new Vector3(0, 0.4f, 0); // teleport player upwards a little to prevent falling through ground
                }
                else
                { 
                    // Exit prone mode
                    _playerAnimator.ResetTrigger("Enter Prone");
                    _playerAnimator.SetTrigger("Exit Prone");
                    _playerAnimator.SetBool("Prone Moving", false);
                    _playerCollider.direction = 1;
                    _playerCollider.height = 3.9f;
                    _playerCollider.center = new Vector3(0, 2, 0);
                    _playerObject.transform.rotation = Quaternion.Euler(new Vector3(0, _playerObject.transform.rotation.y, 0)); // reset x and z angleswe

                    // Reset prone angle data
                    _proneAngleXSpeed = 0;
                    _proneAngleYSpeed = 0;
                    _proneAngleZSpeed = 0;
                }
                
                _isProne = !_isProne;
                _lastProneTime = Time.time;
                _lastMovementInput = new Vector2(0, 0);
            }
        }
        
        // Check for jump input only if enough time has passed since the last jump
        if (Time.time - _lastJumpTime >= JumpDelay && !_isRolling)
        {
            if (_isGrounded && !_isProne && Input.GetKey(Jump))
            { 
                // Player wants to jump
                _playerAnimator.ResetTrigger("Land");
                _playerAnimator.SetTrigger("Jump");
                _isGrounded = false;
                _lastJumpTime = Time.time;

                _playerRigidbody.velocity = new Vector3(_playerRigidbody.velocity.x, JumpForce, _playerRigidbody.velocity.z);
            }
            else
            {
                RaycastHit groundHit;
                var checkGroundStart = _playerObject.transform.position + new Vector3(0, _playerCollider.height / 2, 0);
                var checkGroundDirection = new Vector3(0, -1, 0);
                var checkGroundDist = _playerCollider.height / 2 + 0.15f;
                var checkGroundRay = new Ray(checkGroundStart, checkGroundDirection);
                var checkGround = Physics.SphereCast(checkGroundRay, 0.5f, out groundHit, checkGroundDist);

                // Draw ground check line for visual debugging with editor
                if (checkGround)
                {
                    UnityEngine.Debug.DrawLine(checkGroundStart, checkGroundStart + checkGroundDirection * checkGroundDist, Color.red);
                }
                else
                {
                    UnityEngine.Debug.DrawLine(checkGroundStart, checkGroundStart + checkGroundDirection * checkGroundDist, Color.green);
                }

                if (_isGrounded && !checkGround)
                { 
                    // No longer on the ground
                    if (!_isProne)
                    {
                        _playerAnimator.ResetTrigger("Land");
                        _playerAnimator.SetTrigger("Jump");
                    }
                    _isGrounded = false;
                }
                else if (!_isGrounded && checkGround)
                { 
                    // Just hit ground
                    if (!_isProne)
                    {
                        _playerAnimator.SetTrigger("Land");
                        _lastMovementInput = new Vector2(0, 0);
                    }
                    _isGrounded = true;
                }

                // Re-orient the player to be parallel to the ground
                if (_isGrounded && _isProne)
                {
                    // Accumulator to determine the average of all ground normal vectors
                    var averageNormal = Vector3.zero;
                    
                    for (var xOffset = -1; xOffset <= 1; xOffset++)
                    {
                        for (var zOffset = -1; zOffset <= 1; zOffset++)
                        {
                            RaycastHit hit;
                            var normalExists = true;

                            if (xOffset == 0 && zOffset == 0)
                            {
                                hit = groundHit;
                            }
                            else
                            {
                                var ray = new Ray(checkGroundStart + new Vector3(xOffset / 5f, 0, zOffset / 5f), checkGroundDirection);
                                if (!Physics.Raycast(ray, out hit, checkGroundDist))
                                {
                                    normalExists = false;
                                }
                            }

                            if (normalExists)
                            {
                                averageNormal += hit.normal;
                            }
                        }
                    }
                    averageNormal = averageNormal.normalized; // get normal direction vector

                    // Calculate direction of forward vector (left of player) using normal and player's facing direction
                    var forwardVectorX = (float)Math.Cos((_playerHorizontalAngle - 90) * Math.PI / 180f);
                    var forwardVectorZ = (float)-Math.Sin((_playerHorizontalAngle - 90) * Math.PI / 180f);
                    var forwardVectorY = (-averageNormal.x * forwardVectorX - averageNormal.z * forwardVectorZ) / averageNormal.y;

                    // Calculate smoothed angles
                    var targetAngle = Quaternion.LookRotation(new Vector3(forwardVectorX, forwardVectorY, forwardVectorZ), averageNormal).eulerAngles;
                    var newAngleX = Mathf.SmoothDampAngle(_playerObject.transform.rotation.eulerAngles.x, targetAngle.x, ref _proneAngleXSpeed, ProneAngleSmoothSpeed);
                    var newAngleY = Mathf.SmoothDampAngle(_playerObject.transform.rotation.eulerAngles.y, targetAngle.y, ref _proneAngleYSpeed, ProneAngleSmoothSpeed);
                    var newAngleZ = Mathf.SmoothDampAngle(_playerObject.transform.rotation.eulerAngles.z, targetAngle.z, ref _proneAngleZSpeed, ProneAngleSmoothSpeed);

                    _playerObject.transform.rotation = Quaternion.Euler(newAngleX, newAngleY, newAngleZ);
                }
            }
        }

        PlaceCamera();
    }

    public void Update()
    {
        _playerHorizontalAngle %= 360f;

        // Get mouse movement distance as a percentage of screen size
        var mouseInput = Input.mousePosition;
        var dxPercent = (mouseInput.x - _prevMouseX) / Screen.width;
        var dyPercent = (mouseInput.y - _prevMouseY) / Screen.height;

        // Update mouse positions
        _prevMouseX = mouseInput.x;
        _prevMouseY = mouseInput.y;

        // Get player rotation input from keyboard
        var keyboardRotationInput = 0f;
        
        if (Input.GetKey(TurnLeft))
        {
            keyboardRotationInput--;
        }

        if (Input.GetKey(TurnRight))
        {
            keyboardRotationInput++;
        }
        _playerHorizontalAngle += keyboardRotationInput * HorizontalTurnSpeed * Time.deltaTime;

        if (Input.GetKey(PanNoTurn))
        {
            _horizontalAngle += dxPercent * HorizontalPanSpeed; // full screen-width swipe is 180 degrees
            _horizontalAngle %= 360f;

            _verticalAngle += -dyPercent * VerticalPanSpeed; // full screen-height swipe is 90 degrees
            _verticalAngle = Mathf.Clamp(_verticalAngle, -90f, 90f);

            if (!_isDraggingMouse)
            {
                // Start tracking mouse dragging
                _isDraggingMouse = true;
                Cursor.visible = false;
            }
        }
        else if (Input.GetKey(PanWithTurn))
        {
            _playerHorizontalAngle += dxPercent * HorizontalPanSpeed;

            _verticalAngle += -dyPercent * VerticalPanSpeed; // full screen-height swipe is 90 degrees
            _verticalAngle = Mathf.Clamp(_verticalAngle, -90f, 90f);

            if (!_isDraggingMouse)
            {
                // Start tracking mouse dragging
                _isDraggingMouse = true;
                Cursor.visible = false;
            }
        }
        else
        {
            if (_isDraggingMouse)
            { 
                // Finished mouse dragging
                SetCursorPos(_beforeDragPos.x, _beforeDragPos.y);
                _isDraggingMouse = false;
                Cursor.visible = true;
            }

            if (_lastMovementInput != Vector2.zero && !_isRolling)
            { 
                // If player is walking, reposition camera on player
                if (_horizontalAngle < 0) _horizontalAngle += 360f;
                
                if ((_horizontalAngle > 0 && _horizontalAngle <= AutoCameraRotationSpeed * Time.deltaTime) ||
                    (_horizontalAngle <= 360 && _horizontalAngle >= 360 - AutoCameraRotationSpeed * Time.deltaTime))
                {
                    _horizontalAngle = 0;
                }
                else if (_horizontalAngle > 0 && _horizontalAngle <= 180)
                {
                    _horizontalAngle -= AutoCameraRotationSpeed * Time.deltaTime;
                }
                else if (_horizontalAngle > 180 && _horizontalAngle < 360)
                {
                    _horizontalAngle += AutoCameraRotationSpeed * Time.deltaTime;
                }
            }
        }

        if (!_isDraggingMouse)
        { 
            // update pre-mouse-drag coordinates
            GetCursorPos(out _beforeDragPos);
        }

        // Check mouse scroll data
        var mouseScrollDelta = Input.mouseScrollDelta.y;
        if (mouseScrollDelta != 0)
        { 
            // Zoom in/out required
            _targetCameraDist -= mouseScrollDelta * ScrollZoomVelocity;
            _targetCameraDist = Mathf.Clamp(_targetCameraDist, CameraMinDist, CameraMaxDist);
        }

        // Check if a blink is necessary
        if (Time.time >= _nextBlink)
        {
            _playerAnimator.SetTrigger("Blink"); // play blink animation

            var rand = new System.Random();
            _nextBlink = Time.time + (float)(rand.NextDouble() * (MaxBlinkDelay - MinBlinkDelay) + MinBlinkDelay);
        }

        // Get player movement input
        var movementInput = new Vector2(0, 0); // (right, forward)
        if (Input.GetKey(MoveForward))
        {
            movementInput.y++;
        }

        if (Input.GetKey(MoveBackward))
        {
            movementInput.y--;
        }

        if (Input.GetKey(StrafeLeft))
        {
            movementInput.x--;
        }

        if (Input.GetKey(StrafeRight))
        {
            movementInput.x++;
        }

        // Handle movement animation changes
        if (movementInput != _lastMovementInput && !_isRolling)
        {
            if (_isProne)
            { 
                // Prone animation changes
                if (movementInput == Vector2.zero)
                {
                    _playerAnimator.SetBool("Prone Moving", false);
                }
                else
                {
                    _playerAnimator.SetBool("Prone Moving", true);
                }
            }
            else
            { 
                // Standing animation changes
                if (movementInput.x == 0)
                {
                    if (movementInput.y == 0)
                    {
                        _playerAnimator.SetTrigger("Stop Walking");
                    }

                    if (movementInput.y == 1)
                    {
                        _playerAnimator.SetTrigger("Walk Forward");
                    }
                    else if (movementInput.y == -1)
                    {
                        _playerAnimator.SetTrigger("Walk Backward");
                    }
                }
                else if (movementInput.x == -1)
                { 
                    if (movementInput.y == 0)
                    {
                        _playerAnimator.SetTrigger("Strafe Left");
                    }

                    if (movementInput.y == 1)
                    {
                        _playerAnimator.SetTrigger("Walk Forward Left");
                    }
                    else if (movementInput.y == -1)
                    {
                        _playerAnimator.SetTrigger("Walk Backward Left");
                    }
                }
                else
                {
                    if (movementInput.y == 0)
                    {
                        _playerAnimator.SetTrigger("Strafe Right");
                    }

                    if (movementInput.y == 1)
                    {
                        _playerAnimator.SetTrigger("Walk Forward Right");
                    }
                    else if (movementInput.y == -1)
                    {
                        _playerAnimator.SetTrigger("Walk Backward Right");
                    }
                }
            }
        }

        // Handle movement velocities
        if (!_isRolling)
        {
            Vector2 moveVelocity; // velocity vector before player rotation transformations
            if (movementInput.x == 0)
            {
                if (movementInput.y == 0)
                {
                    moveVelocity = new Vector2(0, 0);
                }
                else if (movementInput.y == 1)
                {
                    moveVelocity = new Vector2(0, ForwardMoveSpeed);
                }
                else
                {
                    moveVelocity = new Vector2(0, -BackwardMoveSpeed);
                }
            }
            else if (movementInput.x == -1)
            {
                if (movementInput.y == 0)
                {
                    moveVelocity = new Vector2(-StrafeSpeed, 0);
                }
                else if (movementInput.y == 1)
                {
                    moveVelocity = new Vector2((float)(-ForwardMoveSpeed * Math.Sin(Math.PI / 4)), (float)(ForwardMoveSpeed * Math.Sin(Math.PI / 4)));
                }
                else
                {
                    moveVelocity = new Vector2((float)(-BackwardMoveSpeed * Math.Sin(Math.PI / 4)), -(float)(BackwardMoveSpeed * Math.Sin(Math.PI / 4)));
                }
            }
            else
            {
                if (movementInput.y == 0)
                {
                    moveVelocity = new Vector2(StrafeSpeed, 0);
                }
                else if (movementInput.y == 1)
                {
                    moveVelocity = new Vector2((float)(ForwardMoveSpeed * Math.Sin(Math.PI / 4)), (float)(ForwardMoveSpeed * Math.Sin(Math.PI / 4)));
                }
                else
                {
                    moveVelocity = new Vector2((float)(BackwardMoveSpeed * Math.Sin(Math.PI / 4)), -(float)(BackwardMoveSpeed * Math.Sin(Math.PI / 4)));
                }
            }
            
            var rotatedMoveVelocity = Quaternion.Euler(0, _playerHorizontalAngle, 0) * (new Vector3(moveVelocity.y * (_isProne ? ProneSpeedMultiplier : 1), _playerRigidbody.velocity.y, -moveVelocity.x * (_isProne ? ProneSpeedMultiplier : 1)));

            // Check if the player can move forward freely
            var moveFreely = true;
            if (!_isProne)
            {
                var velocityRay = new Ray(_playerObject.transform.position + new Vector3(0, 2f, 0), rotatedMoveVelocity.normalized);
                if (Physics.SphereCast(velocityRay, 0.4f, out _, 1.5f))
                {
                    // Cannot move forward due to blockage
                    _playerRigidbody.velocity = new Vector3(0, _playerRigidbody.velocity.y, 0);
                    moveFreely = false;
                }
            }
            else
            {
                var velocityRay = new Ray(_playerObject.transform.position + new Vector3(0, 3.5f, 0), rotatedMoveVelocity.normalized);
                if (Physics.SphereCast(velocityRay, 0.4f, out _, 1.5f))
                { 
                    // Cannot move forward due to blockage
                    _playerRigidbody.velocity = new Vector3(0, _playerRigidbody.velocity.y, 0);
                    moveFreely = false;
                }
            }

            if (moveFreely)
            {
                _playerRigidbody.velocity = rotatedMoveVelocity;
            }
            _lastMovementInput = movementInput;

            // Update player rotation
            if (!_isProne)
            {
                _playerObject.transform.rotation = Quaternion.Euler(_playerObject.transform.rotation.x, _playerHorizontalAngle, _playerObject.transform.rotation.z);
            }
        }
    }

    /// <summary>
    /// Determines where the camera should be placed for the player (call in LateUpdate).
    /// </summary>
    private void PlaceCamera()
    {
        var prevCameraDist = _curCameraDist;

        // Update vertical position
        RaycastHit surfaceHit;
        var checkGroundStart = _playerObject.transform.position + new Vector3(0, _playerCollider.height / 2, 0);
        var checkGroundDirection = new Vector3(0, -1, 0);
        var checkGroundDist = 12f;
        var checkGroundRay = new Ray(checkGroundStart, checkGroundDirection);
        var checkGround = Physics.SphereCast(checkGroundRay, 0.5f, out surfaceHit, checkGroundDist, 1 << 6);
        var playerTargetVerticalPosition = _playerObject.transform.position.y;

        if (checkGround)
        {
            playerTargetVerticalPosition = surfaceHit.point.y;
            UnityEngine.Debug.DrawLine(checkGroundStart, checkGroundStart + checkGroundDirection * checkGroundDist, Color.yellow);
        }

        // Player position for camera (ground underneath)
        var modifiedPlayerPos = new Vector3(_playerObject.transform.position.x, playerTargetVerticalPosition, _playerObject.transform.position.z);

        // Adjust follow position
        var _followPoint2D = Vector2.SmoothDamp(new Vector2(_followPoint.x, _followPoint.z), new Vector2(modifiedPlayerPos.x, modifiedPlayerPos.z), ref _followPointVelocity, 0.25f);
        var _followPointVert = Mathf.SmoothDamp(_followPoint.y, modifiedPlayerPos.y, ref _followPointVerticalVelocity, 0.5f);
        _followPoint = new Vector3(_followPoint2D.x, _followPointVert, _followPoint2D.y);

        // Determine camera position based on camera angles
        var eyePos = new Vector3(0, 3 * _playerScale, 0); // 0.135 = 3 * 0.045, adjusted for player object scale
        var cameraX = _followPoint.x + CameraMaxDist * (float)-Math.Cos((_horizontalAngle + _playerHorizontalAngle) * Math.PI / 180.0) * (float)Math.Cos(_verticalAngle * Math.PI / 180.0);
        var cameraY = _followPoint.y + CameraMaxDist * (float)Math.Sin(_verticalAngle * Math.PI / 180.0) + eyePos.y; // +3 to view from eyes, not feet
        var cameraZ = _followPoint.z + CameraMaxDist * (float)Math.Sin((_horizontalAngle + _playerHorizontalAngle) * Math.PI / 180.0) * (float)Math.Cos(_verticalAngle * Math.PI / 180.0);

        // Check for blocking objects
        RaycastHit hit;
        var lineStart = new Vector3(cameraX, cameraY, cameraZ);
        var lineEnd = _followPoint + eyePos;
        var sweepDirection = (lineStart - lineEnd).normalized;
        lineEnd += sweepDirection * 0.5f; // extra buffer distance behind player
        var sweepDist = Vector3.Distance(lineStart, lineEnd) + 1.5f; // extra buffer distance
        var nextFrameCameraDist = CameraMaxDist;
        var sweepRay = new Ray(lineEnd, sweepDirection);

        if (Physics.SphereCast(sweepRay, 0.5f, out hit, sweepDist))
        {
            UnityEngine.Debug.DrawLine(lineEnd, lineEnd + sweepDirection * sweepDist, Color.red);

            // Calculate camera's supposed distance from player
            nextFrameCameraDist = Vector3.Distance(hit.point, lineEnd) - 1.5F; // spacing between camera and obstructing wall
        }
        else
        {
            UnityEngine.Debug.DrawLine(lineEnd, lineEnd + sweepDirection * sweepDist, Color.green);
        }

        nextFrameCameraDist = Mathf.Clamp(nextFrameCameraDist, CameraMinDist, _targetCameraDist);

        // Readjust camera rotation
        var cameraRotX = _verticalAngle;
        var cameraRotY = _horizontalAngle + 90;
        cameraObject.transform.rotation = Quaternion.Euler(cameraRotX, cameraRotY + _playerHorizontalAngle, 0);

        // Set camera position, with smoothing
        _curCameraDist = Mathf.SmoothDamp(_curCameraDist, nextFrameCameraDist, ref _cameraZoomVelocity, 0.035f);
        var targetPos = _followPoint + sweepDirection * _curCameraDist + eyePos;
        cameraObject.transform.position = targetPos;

        // Set player transparency, but only if camera distance has changed
        if (_curCameraDist != prevCameraDist)
        {
            if (_curCameraDist < 2 * _playerScale && !_isPlayerTransparent)
            {
                _isPlayerTransparent = true;
                _playerRenderer.enabled = false;
            }
            else if (_curCameraDist >= 2 * _playerScale && _isPlayerTransparent)
            {
                _isPlayerTransparent = false;
                _playerRenderer.enabled = true;
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y); // sets cursor position on screen (full screen coordinates, not app screen)

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out AbsoluteMousePosition lpMousePosition); // gets cursor position from full screen coordinates (not app screen)

    [StructLayout(LayoutKind.Sequential)]
    private struct AbsoluteMousePosition // full screen-space coordinates (not app screen)
    {
        public int x;
        public int y;
    }
}

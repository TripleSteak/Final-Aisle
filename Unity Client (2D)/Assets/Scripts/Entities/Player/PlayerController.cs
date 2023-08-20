using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System;
using UnityEngine;
using FinalAisle_Shared.Networking;

/// <summary>
/// Manages the player instance controlled by the local client.
/// </summary>
public sealed class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rigidBody;
    private Animator _animator;
    private Transform _playerTransform;
    private RabbitPortrait _playerPortrait;

    [Tooltip("Speed at which the player can run horizontally, in world space")]
    [SerializeField] 
    private float moveSpeed;
    
    [Tooltip("Layers with which the player can collide")]
    [SerializeField]
    private LayerMask collidableLayers;
    
    [Tooltip("Transform of the GameObject used for collision-based ground detection")]
    [SerializeField]
    private Transform groundCheck;
    
    [Tooltip("The player's rolling speed, as a percentage of regular movement speed")]
    [SerializeField]
    private float rollSpeed;

    private GameObject camera;
    private GameObject connectionObjectt;
    private Connection connection;

    private float _currentMoveVelocity; // horizontal movement velocity this frame

    private bool _isFacingRight = true;
    private bool _shouldAddJumpForce; // whether to add a jump force to the player character on next fixed update
    private bool _isRollingRight = true; // whether the player is rolling towards the right
    private bool _tappedRightLastFrame = true; // if the last arrow key press was to the right
    private bool _isDeepCrouching;

    private float _nextBlinkTime; // Value of Time.time at which the next blink should occur

    private float _timeSinceLastWalk; // number of seconds since the last walk command
    private float _timeSinceLastRoll = 1; // number of seconds since the last roll was initiated
    private float _timeSinceLastLean; // number of seconds since the last instance of movement
    private float _timeSinceLastCrouch; // number of seconds since the last instance of crouching
    
    private float _lastXPosPos; // player's previous X value in world space
    private float _lastClickTime; // Value of Time.time at which the last right or left click occurred

    private const float GroundedRadius = 0.2f; // radius of the sphere used for collision-based ground detection
    private const float RollDuration = 0.45f; // how long a roll lasts, in seconds
    private const float WalkAnimationTimeLimit = 0.1f; // minimum period of inactivity (in seconds) at which walk animation is stopped
    private const float LeanAnimationTimeLImit = 0.1f; // minimum period of inactivity (in seconds) at which walk lean is disactivated
    private const float DoubleClickWindow = 0.4f; // maximum duration (in seconds) between successive clicks to count as double click
    private const float DeepCrouchingChannelingDuration = 2.0f; // number of seconds of crouching until deep crouching begins

    public void Start()
    {
        connection = connectionObjectt.GetComponent<Connection>();

        // Inform server of level join
        connection.SendData(PacketDataUtils.Condense(PacketDataUtils.JoinLevel, ""));

        _rigidBody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerTransform = GetComponent<Transform>();
        _playerPortrait = camera.transform.Find("HUD").transform.Find("Rabbit Player Pastel").GetComponent<RabbitPortrait>();

        _lastXPos = _playerTransform.position.x;

        InitNextBlink();
    }

    public void Update()
    {
        var moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); // left/right for horizontal movement, up/down for climbing/rolling

        var jumpThisFrame = false;
        var rollThisFrame = false;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            // Check for double taps (which are interpreted as rolls)
            if (_tappedRightLastFrame && Time.time - _lastClickTime <= DoubleClickWindow) rollThisFrame = true;

            _tappedRightLastFrame = true;
            _lastClickTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            // Check for double taps (which are interpreted as rolls)
            if (!_tappedRightLastFrame && Time.time - _lastClickTime <= DoubleClickWindow) rollThisFrame = true;

            _tappedRightLastFrame = false;
            _lastClickTime = Time.time;
        }

        if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling") && ((moveInput.x < 0 && _isFacingRight) || (moveInput.x > 0 && !_isFacingRight))) // change movement direction
        {
            _isFacingRight = !_isFacingRight;

            // Reflect the player's sprite horizontally
            Vector3 newScale = new Vector3(moveInput.x, transform.localScale.y, transform.localScale.z);
            _playerTransform.localScale = newScale;
        }

        if (Time.time >= nextBlinkTime)
        {
            _animator.SetTrigger("Blink");
            _playerPortrait.Blink();
            InitNextBlink();
        }

        if (rollThisFrame && _animator.GetBool("Is Grounded") && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling")) // roll
        {
            _animator.SetTrigger("Roll");
            _timeSinceLastRoll = 0;
            
            // Determine which way the player is rolling
            _isRollingRight = moveInput.x == 0 ? _isFacingRight : moveInput.x == 1;
            _isDeepCrouching = false; // uncrouch if rolling
            _timeSinceLastCrouch = -RollDuration;
        }
        else
        {
            rollThisFrame = false;
        }

        if (!rollThisFrame && Input.GetKeyDown(KeyCode.Space) && _animator.GetBool("Is Grounded") && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling"))
        {
            _animator.SetTrigger("Jump");
            _shouldAddJumpForce = true;
            jumpThisFrame = true;
        }

        if (moveInput.x == 0)
        {
            _timeSinceLastWalk += Time.deltaTime;
            if (_timeSinceLastWalk >= walkLimit && _animator.GetBool("Is Walking")) _animator.SetBool("Is Walking", false);
        }
        else
        {
            if (_timeSinceLastWalk != 0) _timeSinceLastWalk = 0;
            if (!_animator.GetBool("Is Walking")) _animator.SetBool("Is Walking", true);
        }

        if (Math.Abs(_lastXPos - _playerTransform.position.x) < 0.05f)
        {
            _timeSinceLastLean += Time.deltaTime;
            if (_timeSinceLastLean >= walkLeanLimit && _animator.GetBool("Is Leaning")) _animator.SetBool("Is Leaning", false);
        }
        else
        {
            if (_timeSinceLastLean != 0) _timeSinceLastLean = 0;
            if (!_animator.GetBool("Is Leaning")) _animator.SetBool("Is Leaning", true);
        }
        
        _lastXPos = _playerTransform.position.x;
        _currentMoveVelocity = moveInput.x * MoveSpeed;

        /*
         * Update player crouching status.
         * The player can only crouch if they are not actively jumping or rolling.
         */
        var attemptCrouch = moveInput.y == -1;
        if (attemptCrouch && !_animator.GetBool("Is Crouching") && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling") && _animator.GetBool("Is Grounded") && !jumpThisFrame)
        {
            _animator.SetBool("Is Crouching", true);
            if (_timeSinceLastCrouch != 0)
            {
                _isDeepCrouching = false;
                _timeSinceLastCrouch = 0;
            }
        }
        else if (!attemptCrouch && _animator.GetBool("Is Crouching"))
        {
            _animator.SetBool("Is Crouching", false);
        }

        // Check if deep crouching should begin
        if(_animator.GetBool("Is Crouching") && !_isDeepCrouching)
        {
            _timeSinceLastCrouch += Time.deltaTime;
            if (_timeSinceLastCrouch > DeepCrouchingChannelingDuration)
            {
                _animator.SetTrigger("Deep Crouch");
                _isDeepCrouching = true;
            }
        }
        
        // Send packets regarding jumping and rolling, if applicable
        if (jumpThisFrame)
        {
            connection.SendData(PacketDataUtils.Condense(PacketDataUtils.MovementJump, ""));
        }
        if (rollThisFrame)
        {
            connection.SendData(PacketDataUtils.Condense(PacketDataUtils.MovementRoll, ""));
        }
    }

    public void FixedUpdate()
    {
        if (_timeSinceLastRoll <= RollDuration) 
        {
            // Calculate player movement speed using rollSpeed modifier
            _rigidBody.velocity = new Vector2(rollSpeed * moveSpeed * (_isRollingRight ? 1 : -1) * Time.fixedDeltaTime, _rigidBody.velocity.y);
        }
        else
        {
            // Preserve the player's horizontal motion
            _rigidBody.velocity = new Vector2(_currentMoveVelocity * (_animator.GetBool("Is Crouching") ? 0 : 1) * Time.fixedDeltaTime, _rigidBody.velocity.y);
        }
        _timeSinceLastRoll += Time.fixedDeltaTime;

        // Send player input data to server { MovementInput:x|y }
        connection.SendData(PacketDataUtils.Condense(PacketDataUtils.MovementInput, moveVelocity / MoveSpeed + "|" + (_animator.GetBool("Is Crouching") ? -1 : 0)));

        // Gravity is added to the player every frame to ensure falling physics
        _rigidBody.AddForce(new Vector2(0, -100), ForceMode2D.Force);
        if (_shouldAddJumpForce)
        {
            _shouldAddJumpForce = false;
            _rigidBody.AddForce(new Vector2(0, 40), ForceMode2D.Impulse);
        }

        var isGrounded = false;
        var colliders = Physics2D.OverlapCircleAll(groundCheck.position, GroundedRadius, collidableLayers);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                isGrounded = true;
            }
        }
        
        if(isGrounded && !_animator.GetBool("Is Grounded"))
        {
            _isDeepCrouching = false;
            _timeSinceLastCrouch = 0;

            // Show landing particles
            Instantiate(camera.GetComponent<PrefabLibrary>().LandingParticles, _playerTransform.position, Quaternion.identity);
        }
        _animator.SetBool("Is Grounded", isGrounded);
    }

    /// <summary>
    /// Determines when the next player blink should be.
    /// </summary>
    private void InitNextBlink()
    {
        nextBlinkTime = Time.time + UnityEngine.Random.Range(0.3f, 3f);
    }
}

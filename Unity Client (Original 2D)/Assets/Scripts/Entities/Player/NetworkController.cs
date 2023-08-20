using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages instances of players controlled by other game clients across the network.
/// </summary>
public sealed class NetworkController : MonoBehaviour
{
    private Rigidbody2D _rigidBody;
    private Animator _animator;
    private Transform _playerTransform;

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

    private float _currentMoveVelocity; // horizontal movement velocity this frame

    private bool _isJumping;
    private bool _isRolling;

    private bool _isFacingRight = true;
    private bool _shouldAddJumpForce; // whether to add a jump force to the player character on next fixed update
    private bool _isRollingRight = true; // whether the player is rolling towards the right

    private float _nextBlinkTime; // Value of Time.time at which the next blink should occur

    private float _timeSinceLastWalk; // number of seconds since the last walk command
    private float _timeSinceLastRoll = 1; // number of seconds since the last roll was initiated
    private float _timeSinceLastLean; // number of seconds since the last instance of movement
    
    private float _lastXPos; // player's previous X value in world space

    private const float GroundedRadius = 0.2f; // radius of the sphere used for collision-based ground detection
    private const float RollDuration = 0.45f; // how long a roll lasts, in seconds
    private const float WalkAnimationTimeLimit = 0.1f; // minimum period of inactivity (in seconds) at which walk animation is stopped
    private const float LeanAnimationTimeLImit = 0.1f; // minimum period of inactivity (in seconds) at which walk lean is disactivated

    /// <summary>
    /// Called during Start() to initialize a network-controlled player
    /// </summary>
    public void Initialize()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerTransform = GetComponent<Transform>();

        _lastXPos = _playerTransform.position.x;

        InitNextBlink();
    }

    /// <summary>
    /// Called by the network to update a foreign player character on Update().
    /// </summary>
    public void RunOnUpdate(Vector2 moveInput)
    {
        if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling") && ((moveInput.x < 0 && _isFacingRight) || (moveInput.x > 0 && !_isFacingRight)))
        {
            _isFacingRight = !_isFacingRight;

            // Reflect the player's sprite horizontally
            Vector3 newScale = new Vector3(moveInput.x, transform.localScale.y, transform.localScale.z);
            _playerTransform.localScale = newScale;
        }

        if (Time.time >= _nextBlinkTime)
        {
            _animator.SetTrigger("Blink");
            InitNextBlink();
        }

        if (_isRolling)
        {
            _animator.SetTrigger("Roll");
            _timeSinceLastRoll = 0;
            
            // Determine which way the player is rolling
            _isRollingRight = moveInput.x == 0 ? _isFacingRight : moveInput.x == 1;
        }
        else if (_isJumping)
        {
            _animator.SetTrigger("Jump");
            _shouldAddJumpForce = true;
        }

        if (moveInput.x == 0)
        {
            _timeSinceLastWalk += Time.deltaTime;
            if (_timeSinceLastWalk >= WalkAnimationTimeLimit && _animator.GetBool("Is Walking")) _animator.SetBool("Is Walking", false);
        }
        else
        {
            if (_timeSinceLastWalk != 0) _timeSinceLastWalk = 0;
            if (!_animator.GetBool("Is Walking")) _animator.SetBool("Is Walking", true);
        }

        if (_lastXPos == _playerTransform.position.x)
        {
            _timeSinceLastLean += Time.deltaTime;
            if (_timeSinceLastLean >= LeanAnimationTimeLImit && _animator.GetBool("Is Leaning")) _animator.SetBool("Is Leaning", false);
        }
        else
        {
            if (_timeSinceLastLean != 0) _timeSinceLastLean = 0;
            if (!_animator.GetBool("Is Leaning")) _animator.SetBool("Is Leaning", true);
        }
        
        _lastXPos = _playerTransform.position.x;
        _currentMoveVelocity = moveInput.x * moveSpeed;

        _isJumping = false;
        _isRolling = false;
    }

    /// <summary>
    /// Called by the network to update a foreign player character on FixedUpdate().
    /// </summary>
    public void RunOnFixedUpdate()
    {
        if (_timeSinceLastRoll <= RollDuration)
        {
            // Calculate player movement speed using rollSpeed modifier
            _rigidBody.velocity = new Vector2(rollSpeed * moveSpeed * (_isRollingRight ? 1 : -1) * Time.fixedDeltaTime, _rigidBody.velocity.y);
        }
        else
        {
            // Preserve the player's horizontal motion
            _rigidBody.velocity = new Vector2(_currentMoveVelocity * Time.fixedDeltaTime, _rigidBody.velocity.y);
        }
        _timeSinceLastRoll += Time.fixedDeltaTime;
        
        // Gravity is added to the player every frame to ensure falling physics
        _rigidBody.AddForce(new Vector2(0, -100), ForceMode2D.Force);
        if (_shouldAddJumpForce)
        {
            _shouldAddJumpForce = false;
            _rigidBody.AddForce(new Vector2(0, 46), ForceMode2D.Impulse);
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
        _animator.SetBool("Is Grounded", isGrounded);
    }

    /// <summary>
    /// Determines when the next player blink should be.
    /// </summary>
    private void InitNextBlink()
    {
        _nextBlinkTime = Time.time + Random.Range(0.3f, 3f);
    }
}

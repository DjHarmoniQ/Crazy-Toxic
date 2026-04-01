using UnityEngine;

/// <summary>
/// PlayerController handles all player movement, jumping, and dash mechanics for the Crazy-Toxic roguelike.
/// Beginner-friendly with detailed comments explaining each system.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 8f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float airDrag = 2f;
    [SerializeField] private float groundDist = 0.5f;
    
    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashCooldown = 0.5f;
    private float lastDashTime = -1f;
    
    // Private variables for movement
    private Rigidbody rb;
    private Vector3 moveDirection;
    private float currentSpeed = 0f;
    private bool isGrounded = false;
    private LayerMask groundLayer;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        groundLayer = LayerMask.GetMask("Ground");
    }
    
    private void Update()
    {
        HandleInput();
        CheckIfGrounded();
        UpdateDrag();
    }
    
    private void FixedUpdate()
    {
        ApplyMovement();
    }
    
    /// <summary>
    /// Handle all player input (movement, jump, dash)
    /// </summary>
    private void HandleInput()
    {
        // Get horizontal input (A/D or Arrow Keys)
        float horizontalInput = Input.GetAxis("Horizontal");
        
        // Set move direction based on input
        if (horizontalInput != 0)
        {
            moveDirection = new Vector3(horizontalInput, 0, 0).normalized;
        }
        
        // Jump input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
        
        // Dash input
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            Dash();
        }
    }
    
    /// <summary>
    /// Check if player is touching the ground using raycast
    /// </summary>
    private void CheckIfGrounded()
    {
        // Cast a ray downward from the player to check if grounded
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundDist, groundLayer);
        
        // Debug visualization
        Debug.DrawRay(transform.position, Vector3.down * groundDist, isGrounded ? Color.green : Color.red);
    }
    
    /// <summary>
    /// Apply smooth movement to the player
    /// </summary>
    private void ApplyMovement()
    {
        float targetSpeed = moveDirection.magnitude * moveSpeed;
        
        // Smoothly accelerate or decelerate
        if (targetSpeed > 0)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.fixedDeltaTime);
        }
        
        // Apply velocity
        Vector3 velocity = rb.velocity;
        velocity.x = moveDirection.x * currentSpeed;
        rb.velocity = velocity;
    }
    
    /// <summary>
    /// Jump mechanic
    /// </summary>
    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, 0);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    
    /// <summary>
    /// Dash mechanic (quick movement boost)
    /// </summary>
    private void Dash()
    {
        lastDashTime = Time.time;
        rb.velocity = new Vector3(moveDirection.x * dashForce, rb.velocity.y, 0);
    }
    
    /// <summary>
    /// Update drag based on whether player is grounded
    /// </summary>
    private void UpdateDrag()
    {
        rb.drag = isGrounded ? groundDrag : airDrag;
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Color Settings")]
    [SerializeField] private Renderer capsuleRenderer;
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private float colorChangeSpeed = 1f;
    [SerializeField] private Color[] colors = new Color[] 
    { 
        Color.red, 
        Color.green, 
        Color.blue, 
    };
    
    [Header("Color-Based Control Modifiers")]
    [SerializeField] private ColorControlModifier[] colorModifiers = new ColorControlModifier[]
    {
        new ColorControlModifier { targetColor = Color.red, gravityMultiplier = 2.0f, jumpForceMultiplier = 1.0f, airControlMultiplier = 0.5f },
        new ColorControlModifier { targetColor = Color.blue, gravityMultiplier = -0.3f, jumpForceMultiplier = 1.5f, airControlMultiplier = 2.0f },
        new ColorControlModifier { targetColor = Color.green, gravityMultiplier = 1.0f, jumpForceMultiplier = 1.0f, airControlMultiplier = 1.0f },
    };
    
    [SerializeField] private float colorMatchThreshold = 0.3f;
    [SerializeField] private float fallingThreshold = -0.5f; // Velocity Y to consider "falling"
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = -1;
    
    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    private Material capsuleMaterial;
    private Color currentColor;
    private bool isGrounded;
    private bool isFalling;
    
    [System.Serializable]
    public class ColorControlModifier
    {
        public Color targetColor;
        [Tooltip("Gravity multiplier - lower = floaty, higher = fast fall")]
        [Range(0.1f, 3f)] public float gravityMultiplier = 1f;
        [Range(0.1f, 3f)] public float jumpForceMultiplier = 1f;
        [Range(0.1f, 3f)] public float airControlMultiplier = 1f;
    }
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Lock rotation so the player doesn't tip over
        rb.freezeRotation = true;
        
        // Get or create material instance
        if (capsuleRenderer == null)
        {
            capsuleRenderer = GetComponent<Renderer>();
        }
        
        if (capsuleRenderer != null)
        {
            capsuleMaterial = capsuleRenderer.material;
        }
        
        // Initialize default colors if array is empty
        if (colors == null || colors.Length == 0)
        {
            colors = new Color[] 
            { 
                Color.red, 
                Color.green, 
                Color.blue, 
            };
        }
    }
    
    void Update()
    {
        // Check for right mouse button click
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
        
        // Check for jump input
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            Jump();
        }
        
        // Update grounded state
        CheckGrounded();
        
        // Check if falling
        isFalling = !isGrounded && rb.velocity.y < fallingThreshold;
        
        // Update color based on position
        UpdateColorFromPosition();
    }
    
    void FixedUpdate()
    {
        // Get current color modifiers
        ColorControlModifier activeModifier = GetActiveColorModifier();
        
        // Apply gravity modifier to the whole object continuously
        if (activeModifier != null)
        {
            // Cancel default gravity and apply modified gravity
            Vector3 customGravity = Physics.gravity * activeModifier.gravityMultiplier;
            rb.AddForce(customGravity - Physics.gravity, ForceMode.Acceleration);
        }
        
        if (isMovingToTarget)
        {
            // Move towards target position
            Vector3 direction = (targetPosition - rb.position).normalized;
            direction.y = 0; // Keep movement on horizontal plane
            
            // Check if we've reached the target
            float distanceToTarget = Vector3.Distance(new Vector3(rb.position.x, 0, rb.position.z), 
                                                     new Vector3(targetPosition.x, 0, targetPosition.z));
            
            if (distanceToTarget < 0.1f)
            {
                isMovingToTarget = false;
            }
            else
            {
                float effectiveSpeed = moveSpeed;
                if (activeModifier != null && !isGrounded)
                {
                    effectiveSpeed *= activeModifier.airControlMultiplier;
                }
                
                rb.MovePosition(rb.position + direction * effectiveSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Move the player with keyboard input
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);
            
            if (movement.magnitude > 0.01f)
            {
                float effectiveSpeed = moveSpeed;
                
                if (activeModifier != null && !isGrounded)
                {
                    // Use air control multiplier when in air
                    effectiveSpeed *= activeModifier.airControlMultiplier;
                }
                
                rb.MovePosition(rb.position + movement * effectiveSpeed * Time.fixedDeltaTime);
            }
        }
    }
    
    private void CheckGrounded()
    {
        // Raycast down to check if grounded
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance + 0.5f, groundLayer);
    }
    
    private void Jump()
    {
        ColorControlModifier activeModifier = GetActiveColorModifier();
        float effectiveJumpForce = jumpForce;
        
        if (activeModifier != null)
        {
            effectiveJumpForce *= activeModifier.jumpForceMultiplier;
        }
        
        rb.AddForce(Vector3.up * effectiveJumpForce, ForceMode.Impulse);
    }
    
    private ColorControlModifier GetActiveColorModifier()
    {
        if (colorModifiers == null || colorModifiers.Length == 0)
            return null;
        
        float closestDistance = float.MaxValue;
        ColorControlModifier closestModifier = null;
        
        foreach (var modifier in colorModifiers)
        {
            float distance = GetColorDistance(currentColor, modifier.targetColor);
            
            if (distance < colorMatchThreshold && distance < closestDistance)
            {
                closestDistance = distance;
                closestModifier = modifier;
            }
        }
        
        return closestModifier;
    }
    
    private float GetColorDistance(Color a, Color b)
    {
        // Calculate Euclidean distance in RGB space
        float rDiff = a.r - b.r;
        float gDiff = a.g - b.g;
        float bDiff = a.b - b.b;
        return Mathf.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
    }
    
    private void UpdateColorFromPosition()
    {
        if (capsuleMaterial == null || colors.Length == 0) return;
        
        Vector3 pos = transform.position;
        
        // Generate Perlin noise based on world position
        float noiseValue = Mathf.PerlinNoise(
            pos.x * noiseScale * colorChangeSpeed * 0.1f,
            pos.z * noiseScale  * colorChangeSpeed * 0.1f
        );
        
        // Add another octave of noise for more variation
        noiseValue += Mathf.PerlinNoise(
            pos.x * noiseScale * 2f * colorChangeSpeed * 0.05f,
            pos.z * noiseScale * 2f  * colorChangeSpeed * 0.05f
        ) * 0.5f;
        
        // Normalize the noise value
        noiseValue = Mathf.Clamp01(noiseValue / 1.5f);
        
        // Get color from array using lerping
        currentColor = GetColorFromArray(noiseValue);
        capsuleMaterial.color = currentColor;
    }
    
    private Color GetColorFromArray(float t)
    {
        if (colors.Length == 0)
            return Color.white;
        
        if (colors.Length == 1)
            return colors[0];
        
        // Map t to the color array
        float scaledValue = t * (colors.Length - 1);
        int lowerIndex = Mathf.FloorToInt(scaledValue);
        int upperIndex = Mathf.CeilToInt(scaledValue);
        
        // Clamp indices
        lowerIndex = Mathf.Clamp(lowerIndex, 0, colors.Length - 1);
        upperIndex = Mathf.Clamp(upperIndex, 0, colors.Length - 1);
        
        // Calculate lerp factor between the two colors
        float lerpFactor = scaledValue - lowerIndex;
        
        // Lerp between the two adjacent colors
        return Color.Lerp(colors[lowerIndex], colors[upperIndex], lerpFactor);
    }
    
    private void HandleRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            targetPosition = hit.point;
            targetPosition.y = rb.position.y; // Keep same height
            isMovingToTarget = true;
        }
    }
    
    // This method is called by the Input System
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        
        // Cancel target movement if player uses keyboard input
        if (moveInput.magnitude > 0.1f)
        {
            isMovingToTarget = false;
        }
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Show ground check ray
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * (groundCheckDistance + 0.5f));
        
        // Show current color modifier state
        ColorControlModifier active = GetActiveColorModifier();
        if (active != null && isFalling)
        {
            Gizmos.color = active.targetColor;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
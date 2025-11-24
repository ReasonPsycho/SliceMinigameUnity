
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Lock rotation so the player doesn't tip over
        rb.freezeRotation = true;
    }
    
    void Update()
    {
        // Check for right mouse button click
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }
    
    void FixedUpdate()
    {
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
                rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Move the player with keyboard input
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
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
}
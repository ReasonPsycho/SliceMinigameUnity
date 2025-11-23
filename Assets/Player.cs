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
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Lock rotation so the player doesn't tip over
        rb.freezeRotation = true;
    }
    
    void FixedUpdate()
    {
        // Move the player
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
    
    // This method is called by the Input System
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}
using UnityEngine;

public class FallStopper : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (spawnPoint != null)
            {
                other.transform.position = spawnPoint.position;
                
                // Optional: Reset velocity if the player has a Rigidbody
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                var vector3 = other.transform.position;
                vector3.y = 40;
                other.transform.position = vector3; 
                
                // Optional: Reset velocity if the player has a Rigidbody
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

            }
        }
    }
}
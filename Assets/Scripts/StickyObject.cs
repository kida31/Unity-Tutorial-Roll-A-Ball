using System.Linq;
using UnityEngine;

// Utility "Tag"
public class StickyObject : MonoBehaviour
{
    public float weight;
    public bool IsGrounded { get; private set; } = false;
    
    public bool IsKinematic => GetComponent<Rigidbody>().isKinematic;
    void OnCollisionStay(Collision collision)
    {
        IsGrounded = collision.contacts.Any(c => c.otherCollider.name == "Ground");
    }
}

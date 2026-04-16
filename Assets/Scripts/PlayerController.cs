using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float speed = 0;
    public TextMeshProUGUI countText;
    public GameObject winTextObject;

    private Rigidbody rb;
    private float movementX;
    private float movementY;

    private int count;

    private int totalPickupCount;

    public float attachmentOffset = 0.5f;
    public float attachmentOffsetFraction = 0.5f;
    public float colliderCooldown = 0.2f;
    public float countSpeedBoost = 0.1f;
    
    private HashSet<GameObject> _stickyObjects = new();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        count = 0;
        totalPickupCount = GameObject.FindGameObjectsWithTag("PickUp").Length;

        SetCountText();
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    void OnCancel(InputValue value)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void FixedUpdate()
    {
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        var speedMult = speed * Math.Pow(1.0f + countSpeedBoost, count);
        rb.AddForce(movement * (float)speedMult);
        
        var isOnFloor = rb.linearVelocity.y < 0.1;
        
        if (!isOnFloor) return;
        var rollingSpeed = Math.Min(rb.angularVelocity.sqrMagnitude, 10) * 0.01f;
        foreach (var stickyObject in _stickyObjects)
        {
            var dPos = transform.position - stickyObject.transform.position;
            if (dPos.y < 0) continue; // Only compress objects below player
            
            var sqdY = dPos.y * Math.Abs(dPos.y);
            var distanceBonus = math.remap(0, 6, 0, 3, sqdY);
            stickyObject.transform.position = Vector3.Lerp(stickyObject.transform.position, transform.position, distanceBonus * rollingSpeed * Time.fixedDeltaTime);    
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PickUp"))
        {
            other.gameObject.tag = "Untagged";
            
            // Debug.Log("PickUp");
            // disable object functionality.
            // other.gameObject.SetActive(false);
            var col = other.GetComponent<Collider>();
            if (col) col.isTrigger = false;
            
            other.GetComponent<Rotator>().enabled = false;
            
            count++;
            SetCountText();
        }
    }

    void SetCountText()
    {
        countText.text = "Count: " + count.ToString();
        if (count >= totalPickupCount)
        {
            winTextObject.SetActive(true);
            // winTextObject.GetComponent<TextMeshProUGUI>().text = "You Win!";
            Destroy(GameObject.FindGameObjectWithTag("Enemy"));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Destroy the current object
            Destroy(gameObject);
            // Update the winText to display "You Lose!"
            winTextObject.gameObject.SetActive(true);
            winTextObject.GetComponent<TextMeshProUGUI>().text = "You Lose!";
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        var stickyThing = collision.gameObject.GetComponent<StickyObject>();
        if (stickyThing && stickyThing.weight <= count)
        {
            Destroy(stickyThing);
            Attach(collision);
            _stickyObjects.Add(collision.gameObject);
            
            // Briefly disallow object to "touch" other things,
            // avoids instantly picking up big patches of objects
            collision.collider.enabled = false;
            StartCoroutine(ActivateColliderLater(collision.collider));
        }
    }

    private void Attach(Collision c)
    {
        var col = c.collider;
        var t = c.transform;
        // Debug.Log("Sticky! " +  t.name);
        t.SetParent(transform);

        var collisionPos = rb.ClosestPointOnBounds(t.position);
        var towardsCollision = (collisionPos - transform.position).normalized;
        var colliderThickness = col.bounds.extents.magnitude;
        // t.position = collisionPos; // stick object center to collision point (surface?)
        t.position += towardsCollision * attachmentOffset;
        t.position += towardsCollision * attachmentOffsetFraction * colliderThickness;

        var tRb = t.GetComponent<Rigidbody>();
        if (tRb) Destroy(tRb);

        var tObstacleComp = t.GetComponent<NavMeshObstacle>();
        if (tObstacleComp) tObstacleComp.enabled = false;
    }

    private IEnumerator ActivateColliderLater(Collider c)
    {
        yield return new WaitForSeconds(colliderCooldown);
        c.enabled = true;
    }
}
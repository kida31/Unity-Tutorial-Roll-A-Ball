using System;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.AI;

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
    public float colliderCooldown = 0.2f;
    public float countSpeedBoost = 0.1f;

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

    void FixedUpdate()
    {
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        var speedMult = speed * Math.Pow(1.0f + countSpeedBoost, count);
        rb.AddForce(movement * (float)speedMult);
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

        var isSticky = collision.gameObject.GetComponent<StickyObject>();
        if (isSticky && isSticky.weight < count)
        {
            // stick object center to surface
            StartCoroutine(SleepCollider(collision.collider));
            Attach(collision.transform);
        }
    }

    private void Attach(Transform t)
    {
        // Debug.Log("Sticky! " +  t.name);
        t.SetParent(transform);

        var collisionPos = rb.ClosestPointOnBounds(t.position);
        var towardsCollision = (collisionPos - transform.position).normalized;
        t.position = collisionPos + towardsCollision * attachmentOffset;

        var tRb = t.GetComponent<Rigidbody>();
        if (tRb) Destroy(tRb);

        var tObstacleComp = t.GetComponent<NavMeshObstacle>();
        if (tObstacleComp) tObstacleComp.enabled = false;

        // other.position =  transform.position + direction * 0.5f;
    }

    private IEnumerator SleepCollider(Collider c)
    {
        c.enabled = false;
        yield return new WaitForSeconds(colliderCooldown);
        c.enabled = true;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public float YPosition = 5f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.gameObject.GetComponent<PlayerRules.PlayerMovement>() != null) 
        {
            Rigidbody rb = other.transform.gameObject.GetComponent<Rigidbody>();
            rb.velocity = new Vector3(0f, 0f, 0f);
            other.transform.position = new Vector3(0f, YPosition, 0f);
        }
    }
}

// Copyright (c) Virtuous. Licensed under the MIT license.
// See LICENSE.md in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchPad : MonoBehaviour
{
    [Header("Jump Pad")]
    public float jumpPadForce = 60f;
    public float plusPositionY = 4f;
    public float lerpSpeed = 0.2f;
    private bool shouldAnimate = false;
    private Vector3 originalPosition;
    private Vector3 newPosition;
    private Vector3 lerpFactor;
    [Space]
    public Rigidbody rb;

    private void Awake() 
    {
        originalPosition = transform.position;

        newPosition.x = transform.position.x;
        newPosition.y = transform.position.y + plusPositionY;
        newPosition.z = transform.position.z;

        lerpFactor = new Vector3(newPosition.x - (newPosition.x * 0.5f), 
            newPosition.y - (newPosition.y * 0.5f), 
            newPosition.z - (newPosition.z * 0.5f));
    }

    private void Update() 
    {
        if (shouldAnimate == true) 
        {
            transform.position = Vector3.Lerp(transform.position, newPosition, lerpSpeed);
            if (transform.position.magnitude >= lerpFactor.magnitude)
                shouldAnimate = false;
        }
        else 
            transform.position = Vector3.Lerp(transform.position, originalPosition, lerpSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        shouldAnimate = true;
        rb = other.transform.gameObject.GetComponent<Rigidbody>();
        rb.AddForce(0, jumpPadForce, 0, ForceMode.Impulse);
    }
}

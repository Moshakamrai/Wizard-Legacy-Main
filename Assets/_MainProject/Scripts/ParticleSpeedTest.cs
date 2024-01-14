using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSpeedTest : MonoBehaviour
{
    private Vector3 lastPosition;
    public float deactivationThreshold = 0.1f;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        CheckMovement();
    }

    void CheckMovement()
    {
        // Check if the position change is below the threshold
        if (Vector3.Distance(transform.position, lastPosition) < deactivationThreshold)
        {
            DeactivateGameObject();
        }

        // Update the last position
        lastPosition = transform.position;
    }

    void DeactivateGameObject()
    {
        gameObject.SetActive(false);
        // Optionally, perform additional actions when deactivating the object
    }
}

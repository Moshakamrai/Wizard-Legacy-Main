using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldSpell : MonoBehaviour
{
    [SerializeField] private Transform castPoint;
    [SerializeField] private GameObject shieldedObject;
    public bool shieldActive;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ActivateShield();
        }
    }

    private void ActivateShield()
    {
        ParticleManager.Instance.PlayParticle("Shield1", shieldedObject.transform.position, shieldedObject.transform.rotation, shieldedObject.transform);
        shieldActive = true;
        // Start a coroutine to deactivate the shieldedObject after 2 seconds
        StartCoroutine(DeactivateAfterDelay(2f));
    }

    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        shieldActive = false;
        // Deactivate the shieldedObject
        shieldedObject.SetActive(false);
    }
}




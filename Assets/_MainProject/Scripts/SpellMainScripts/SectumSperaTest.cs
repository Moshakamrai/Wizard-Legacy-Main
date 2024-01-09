using Cinemachine.Utility;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectumSperaTest : MonoBehaviour
{
    [SerializeField]
    private Transform castPoint;
    [SerializeField]
    private GameObject castObject;

    [SerializeField]
    public bool castedSpell;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Use the center of the camera's viewport as the ray origin
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red, 1f);
            if (Physics.Raycast(ray, out hit))
            {
                CastSectumSepra(hit.collider.gameObject.transform);
            }
            else
            {
                // If no object is hit, cast the spell at a specific distance along the ray
                float castDistance = 40f; // Adjust the distance as needed
                Vector3 castPoint = ray.origin + ray.direction * castDistance;

                // Create a dummy transform at the cast point
                Transform dummyTransform = new GameObject("DummyTransform").transform;
                dummyTransform.position = castPoint;

                // Perform the spell casting at the specified position
                CastSectumSepra(dummyTransform);

                // Optionally, you may want to destroy the dummyTransform after use
                dummyTransform.gameObject.SetActive(false);
            }
        }
    }

    public void CastSectumSepra(Transform target)
    {
        GameObject spellCastObject = Instantiate(castObject, castPoint.position, Quaternion.identity);
        ParticleManager.Instance.PlayParticle("FirstProjectile", spellCastObject.transform.position, transform.rotation, spellCastObject.transform);

        float distance = Vector3.Distance(transform.position, target.transform.position);
        float flyDuration = distance / 25f; // Adjust the divisor to control the speed


        // Fly towards the target
        spellCastObject.transform.DOMove(target.transform.position, flyDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                //LeviosaActivate();
                spellCastObject.SetActive(false);
            });
    }
}

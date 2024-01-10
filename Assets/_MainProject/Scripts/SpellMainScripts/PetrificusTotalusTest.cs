using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetrificusTotalusTest : MonoBehaviour
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
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Use the center of the camera's viewport as the ray origin
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            

            ParticleManager.Instance.PlayParticle("CastingEffectPetri", castPoint.transform.position + new Vector3(0f, 0f, 0f), castPoint.rotation * Quaternion.Euler(80, 0, 90), castPoint.transform);
            RaycastHit hit;
            Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red, 1f);
            if (Physics.Raycast(ray, out hit))
            {
                CastPetrificus(hit.collider.gameObject.transform);
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
                CastPetrificus(dummyTransform);

                // Optionally, you may want to destroy the dummyTransform after use
                dummyTransform.gameObject.SetActive(false);
            }
        }
    }

    public void CastPetrificus(Transform target)
    {
        GameObject spellCastObject = Instantiate(castObject, castPoint.position, Quaternion.identity);

        ParticleManager.Instance.PlayParticle("PetriProjectile", castPoint.transform.position + new Vector3(0f, 0f, 6f), castPoint.transform.rotation * Quaternion.Euler(90, 0, 0), spellCastObject.transform);

        float distance = Vector3.Distance(transform.position, target.transform.position);
        float flyDuration = distance / 11f; // Adjust the divisor to control the speed
        float randomX = Random.Range(-2f, 2f);
        float randomY = Random.Range(0.2f, 1f);

        // Modify the control points to include continuous x-axis rotation
        Vector3[] pathPoints = new Vector3[5];
        pathPoints[0] = spellCastObject.transform.position;
        pathPoints[1] = spellCastObject.transform.position + new Vector3(randomX, randomY, 0f); // Control point 1
        pathPoints[2] = spellCastObject.transform.position + new Vector3(0f, 0f, 0f); // Rotation control point
        pathPoints[3] = spellCastObject.transform.position + new Vector3(randomX, randomY, 0f); // Control point 2
        pathPoints[4] = target.transform.position;

        // Fly towards the target with curved movement and rotation
        spellCastObject.transform.DOLocalPath(pathPoints, flyDuration, PathType.CatmullRom, PathMode.Full3D, 10, Color.red)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                // Continuous rotation around the x-axis
                spellCastObject.transform.Rotate(Vector3.forward, Time.deltaTime * 10f); // Adjust the rotation speed as needed
            })
            .OnComplete(() =>
            {
                //LeviosaActivate();
                spellCastObject.SetActive(false);
            });
    }





    Vector3 CalculateMidpoint(Vector3 point1, Vector3 point2)
    {
        // Calculate a midpoint between two points
        return (point1 + point2) / 2f;
    }

}

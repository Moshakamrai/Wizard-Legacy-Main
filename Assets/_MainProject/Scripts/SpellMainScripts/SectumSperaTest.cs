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

    [SerializeField] 
    float spellSpeed;
    [SerializeField] 
    private SpellData spellDatas;

    private static SectumSperaTest instance;

    public static SectumSperaTest Instance
    {
        get
        {
            if (instance == null)
            {
                // If no instance exists, create one
                GameObject singletonObject = new GameObject("SectumSperaTest");
                instance = singletonObject.AddComponent<SectumSperaTest>();
            }

            return instance;
        }
    }

    void Awake()
    {
        // Ensure only one instance of the class exists
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        spellSpeed = spellDatas.spellSpeed;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            castedSpell = true;
            // Use the center of the camera's viewport as the ray origin
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red, 1f);
            if (Physics.Raycast(ray, out hit))
            {
                CastSectumSepra(hit.collider.gameObject.transform);
                Debug.Log("Got the target");

            }
            else
            {
                // If no object is hit, cast the spell at a specific distance along the ray
                float castDistance = 100f; // Adjust the distance as needed
                Vector3 castPoint = ray.origin + ray.direction * castDistance;

                // Create a dummy transform at the cast point
                Transform dummyTransform = new GameObject("DummyTransform").transform;
                dummyTransform.position = castPoint;

                // Perform the spell casting at the specified position
                CastSectumSepra(dummyTransform);

                // Optionally, you may want to destroy the dummyTransform after use
                dummyTransform.gameObject.SetActive(false);
                castedSpell = true;
                Debug.Log("creating dummy");
            }
        }
    }
    public void FireSectrumSpera()
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

    public void CastSectumSepra(Transform target)
    {
        GameObject spellCastObject = Instantiate(castObject, castPoint.position, Quaternion.identity);
        ParticleManager.Instance.PlayParticle("FirstProjectile", castPoint.transform.position + new Vector3(0f, 0f, 4f), transform.rotation, spellCastObject.transform);

        float distance = Vector3.Distance(castPoint.position, target.transform.position);
        float flyDuration = distance / spellSpeed; // Adjust the divisor to control the speed
        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-0.5f, 0.8f);

        Vector3[] pathPoints;

        float minDeviationDistance = 3f; // Adjust this value based on your requirements

        if (distance > minDeviationDistance)
        {
            // Use curved movement with deviation
            pathPoints = new Vector3[3];
            pathPoints[0] = castPoint.transform.position;
            pathPoints[1] = castPoint.transform.position + new Vector3(randomX, randomY, 0f); // Control point 1
            pathPoints[2] = target.transform.position;
        }
        else
        {
            // Go directly towards the target without deviation
            pathPoints = new Vector3[2];
            pathPoints[0] = castPoint.transform.position;
            pathPoints[1] = target.transform.position;
        }

        // Fly towards the target with curved movement or direct movement
        spellCastObject.transform.DOLocalPath(pathPoints, flyDuration, PathType.CatmullRom, PathMode.Full3D, 10, Color.red)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                //spellCastObject.SetActive(false);
                castedSpell = true;
                //ParticleManager.Instance.PlayParticle("FirstProjectileExplosion", target.position, transform.rotation);
            });
    }

}

using Cinemachine.Utility;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SectumSperaTest : MonoBehaviour
{
    [SerializeField]
    private Transform castPoint;
    [SerializeField]
    private GameObject castObject;

    [SerializeField]
    private bool castedSpell;

    [SerializeField]
    private float spellSpeed;

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

    private void Awake()
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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            castedSpell = true;
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.CompareTag("CollideObject"))
            {
                CastSectumSepra(hit.collider.gameObject.transform);
                Debug.Log("Got the target");
            }
            else
            {
                float castDistance = 40f;
                Vector3 castPoint = ray.origin + ray.direction * castDistance;
                Transform dummyTransform = new GameObject("DummyTransform").transform;
                dummyTransform.position = castPoint;

                CastSectumSepra2(dummyTransform);
                castedSpell = true;
                dummyTransform.gameObject.SetActive(false);
            }
        }
    }

    public void FireSectrumSpera()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            CastSectumSepra(hit.collider.gameObject.transform);
        }
        else
        {
            float castDistance = 40f;
            Vector3 castPoint = ray.origin + ray.direction * castDistance;
            Transform dummyTransform = new GameObject("DummyTransform").transform;
            dummyTransform.position = castPoint;

            CastSectumSepra(dummyTransform);

            dummyTransform.gameObject.SetActive(false);
        }
    }

    public void CastSectumSepra(Transform target1)
    {
        Transform target = target1.gameObject.GetComponent<SkeletonStateMachine>().hitPoint.transform;
        GameObject spellCastObject = Instantiate(castObject, castPoint.position, Quaternion.identity);
        ParticleManager.Instance.PlayParticle("FirstProjectile", castPoint.position, transform.rotation, spellCastObject.transform);

        float distance = Vector3.Distance(castPoint.position, target.transform.position);
        float flyDuration = distance / spellSpeed;
        float randomX = Random.Range(0f, 1f);

        Vector3[] pathPoints;

        float minDeviationDistance = 3f;

        if (distance > minDeviationDistance)
        {
            pathPoints = new Vector3[3];
            pathPoints[0] = castPoint.transform.position;
            pathPoints[1] = castPoint.transform.position + new Vector3(randomX, 0f, 0f); // Control point 1
            pathPoints[2] = target.transform.position;
            Debug.Log("Should deviate");
        }
        else
        {
            pathPoints = new Vector3[2];
            pathPoints[0] = castPoint.transform.position;
            pathPoints[1] = target.transform.position;
            Debug.Log("Should not deviate");
        }

        spellCastObject.transform.DOLocalPath(pathPoints, flyDuration, PathType.CatmullRom, PathMode.Full3D, 10, Color.red)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                castedSpell = true;
            });
    }
    public void CastSectumSepra2(Transform target)
    {
        //Transform target = target1.gameObject.GetComponent<SkeletonStateMachine>().hitPoint.transform;
        GameObject spellCastObject = Instantiate(castObject, castPoint.position, Quaternion.identity);
        ParticleManager.Instance.PlayParticle("FirstProjectile", castPoint.position, transform.rotation, spellCastObject.transform);

        float distance = Vector3.Distance(castPoint.position, target.transform.position);
        float flyDuration = distance / spellSpeed;
        float randomX = Random.Range(0f, 1f);

        Vector3[] pathPoints;

        float minDeviationDistance = 3f;

        if (distance > minDeviationDistance)
        {
            pathPoints = new Vector3[3];
            pathPoints[0] = castPoint.transform.position;
            pathPoints[1] = castPoint.transform.position + new Vector3(randomX, 0f, 0f); // Control point 1
            pathPoints[2] = target.transform.position;
            Debug.Log("Should deviate");
        }
        else
        {
            pathPoints = new Vector3[2];
            pathPoints[0] = castPoint.transform.position;
            pathPoints[1] = target.transform.position;
            Debug.Log("Should not deviate");
        }

        spellCastObject.transform.DOLocalPath(pathPoints, flyDuration, PathType.CatmullRom, PathMode.Full3D, 10, Color.red)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                castedSpell = true;
            });
    }
}

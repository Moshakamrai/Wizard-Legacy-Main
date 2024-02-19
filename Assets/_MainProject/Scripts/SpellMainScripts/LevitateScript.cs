using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public class LevitateScript : MonoBehaviour
{
    [SerializeField]
    private float levitationDuration = 1.5f;  // Set the duration for levitation
    [SerializeField]
    private float rotationAmount = 360f;  // Set the amount of rotation in degrees
    [SerializeField]
    private float rotationDuration = 1f;  // Set the duration for rotation
    [SerializeField]
    public SpellData spellDatas;

    [SerializeField]
    private bool auraRun;

    [SerializeField]
    private GameObject auraGameObject;

    [SerializeField]
    public bool attackMode;

    [SerializeField]
    private float spellSpeed;

    // Start is called before the first frame update
    void Start()
    {
        spellSpeed = spellDatas.spellSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LevitateUpwards(GameObject target)
    {
        // Levitate upwards
        //transform.DOMoveY(levitationHeight, levitationDuration).SetEase(Ease.InOutQuad);

        // Rotate during levitation
        transform.DORotate(new Vector3(0f, rotationAmount, 0f), rotationDuration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);

        float distance = Vector3.Distance(transform.position, target.transform.position);
        float flyDuration = distance / 20f;
        transform.DOMove(target.transform.position, flyDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Debug.Log("Reached the target!");
                transform.parent = target.transform;
            });
        ParticleManager.Instance.PlayParticle("LevitateAura", gameObject.transform.position, transform.rotation, auraGameObject.transform);
    }

    public void FlyTowards(GameObject target1)
    {
        Transform target = target1.gameObject.GetComponent<SkeletonStateMachine>().hitPoint.transform;
        transform.parent = null;
        attackMode = true;
        DOTween.Clear();
        ParticleManager.Instance.PlayParticle("TrailLevitate", gameObject.transform.position, transform.rotation, auraGameObject.transform);

        float distance = Vector3.Distance(transform.position, target.transform.position);
        float flyDuration = distance / 20f; // Adjust the divisor to control the speed

        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-0.5f, 0.8f);

        Vector3[] pathPoints = new Vector3[3];
        pathPoints[0] = transform.position;
        pathPoints[1] = transform.position + new Vector3(randomX, randomY, 0f); // Control point 1
        pathPoints[2] = target.position;

        // Fly towards the target with curved movement
        transform.DOLocalPath(pathPoints, flyDuration, PathType.CatmullRom, PathMode.Full3D, 10, Color.red)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Debug.Log("Reached the target!");
            });
    }





    private void OnCollisionEnter(Collision collision)
    {
        Transform target = collision.gameObject.GetComponent<SkeletonStateMachine>().hitPoint.transform;
        if (collision.gameObject.CompareTag("CollideObject") && attackMode)
        {
            // Perform actions or call methods when collision with "CollideObject" occurs
            Debug.Log("Collided with an object with the tag 'CollideObject'");

            // Example: Deactivate the collided object
            
            ParticleManager.Instance.PlayParticle("BlueExplosion", target.transform.position , transform.rotation);
            gameObject.SetActive(false);
          
            //collision.gameObject.SetActive(false);
        }
        else if (collision.gameObject.CompareTag("StaticObjects") && attackMode)
        {
            ParticleManager.Instance.PlayParticle("BlueExplosion", gameObject.transform.position, transform.rotation);
            gameObject.SetActive(false);            
            
        }
    }
}


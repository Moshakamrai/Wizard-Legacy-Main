using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;

public class LevitateScript : MonoBehaviour
{
    [SerializeField]
    private float levitationHeight = 3f;  // Set the desired levitation height
    [SerializeField]
    private float levitationDuration = 1.5f;  // Set the duration for levitation
    [SerializeField]
    private float rotationAmount = 360f;  // Set the amount of rotation in degrees
    [SerializeField]
    private float rotationDuration = 1f;  // Set the duration for rotation

    [SerializeField]
    private bool auraRun;

    [SerializeField]
    private GameObject auraGameObject;

    [SerializeField]
    private bool attackMode;

    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LevitateUpwards(GameObject target)
    {
        // Levitate upwards
        transform.DOMoveY(levitationHeight, levitationDuration).SetEase(Ease.InOutQuad);

        // Rotate during levitation
        transform.DORotate(new Vector3(0f, rotationAmount, 0f), rotationDuration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);

        float distance = Vector3.Distance(transform.position, target.transform.position);
        float flyDuration = distance / 20;
        transform.DOMove(target.transform.position, flyDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Debug.Log("Reached the target!");
                transform.parent = target.transform;
            });
        ParticleManager.Instance.PlayParticle("LevitateAura", gameObject.transform.position, transform.rotation, auraGameObject.transform);
    }

    public void FlyTowards(GameObject target) 
    {
        attackMode = true;
        DOTween.Clear();
        ParticleManager.Instance.PlayParticle("TrailLevitate", gameObject.transform.position, transform.rotation, auraGameObject.transform);
        // Calculate the duration based on the distance to the target
        float distance = Vector3.Distance(transform.position, target.transform.position);
        float flyDuration = distance / 20; // Adjust the divisor to control the speed

        
        // Fly towards the target
        transform.DOMove(target.transform.position, flyDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Debug.Log("Reached the target!");      
            });
    }

    


    private void OnCollisionEnter(Collision collision)
    {
        // Check if the colliding object has the tag "CollideObject"
        if (collision.gameObject.CompareTag("CollideObject") && attackMode)
        {
            // Perform actions or call methods when collision with "CollideObject" occurs
            Debug.Log("Collided with an object with the tag 'CollideObject'");

            // Example: Deactivate the collided object
            
            ParticleManager.Instance.PlayParticle("BlueExplosion", gameObject.transform.position , transform.rotation);
            gameObject.SetActive(false);
            collision.gameObject.SetActive(false);
        }
        else if (collision.gameObject.CompareTag("StaticObjects") && attackMode)
        {
            ParticleManager.Instance.PlayParticle("BlueExplosion", gameObject.transform.position, transform.rotation);
            gameObject.SetActive(false);            
            
        }
    }
}


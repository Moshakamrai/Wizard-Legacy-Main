using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class SkeletonStateMachine : MonoBehaviour
{
    SkeletonStates currentState;
    internal WalkTowardSK walktoward = new WalkTowardSK();
    internal GetHurtSK getHurt = new GetHurtSK();
    internal AttackSK attack = new AttackSK();

    #region Attributes
    internal float enemyHealth;
    internal int hitCounter;
    public bool resetSK;
    internal bool isWalkSKPlaying;
    internal float proximityThreshold;
    internal float pushBackForce; // The force of the pushback
    internal float pushBackDuration = 0.5f;
    #endregion

    #region Components
    [SerializeField] internal Animator animSK;
    [SerializeField] internal GameObject mainPlayer;
    [SerializeField] internal NavMeshAgent navMeshAgent;
    public GameObject hitPoint;
    #endregion

    private void Start()
    {
        enemyHealth = 100;
        proximityThreshold = 2f;
        currentState = walktoward;
        pushBackForce = 2f;
        currentState.EnterState(this);
    }
    public void SwitchState(SkeletonStates state)
    {
        currentState = state;
        state.EnterState(this);
    }

    private void Update()
    {
        currentState.UpdateState(this);
        //isWalkSKPlaying = IsAnimationStatePlaying("WalkSK");
        if (enemyHealth < 0)
        {
            gameObject.SetActive(false);
        }
        

    }
    public void SetDestination(Vector3 targetPosition)
    {
        if (navMeshAgent != null)
        {
            // Reset the NavMeshAgent before setting a new destination
            navMeshAgent.isStopped = false;

            // Set the new destination for the NavMeshAgent
            navMeshAgent.SetDestination(targetPosition);
        }
        else
        {
            Debug.LogError("NavMeshAgent component not found.");
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (gameObject.GetComponent<Outline>())
        {
            gameObject.GetComponent<Outline>().enabled = false;
        }
        ParticleManager.Instance.PlayParticle("FirstProjectileExplosion", hitPoint.transform.position, transform.rotation);
        SpellDamageControl(other);
        other.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<LevitateScript>() != null)
        {
            if (collision.gameObject.GetComponent<LevitateScript>().attackMode)
            {
                SpellDamageControl(collision.gameObject);
                hitCounter = 3;
            }    
        }   
    }

    public void SpellDamageControl(GameObject spellInfo)
    {
        if (spellInfo.GetComponent<SpellDataRetrieve>())
        {
            
            int damageStat = spellInfo.GetComponent<SpellDataRetrieve>().damage;
            enemyHealth -= damageStat;
            if (damageStat == 1)
            {
                hitCounter = 3;
                Debug.Log("got shielded");
            }
            else
            {
                hitCounter += 1;
            }
            
        }
    }

    public void DamageState()
    {

        Debug.Log("should Damage player");


    }
    public void ApplyPushBack()
    {
        StartCoroutine(PushBack());
    }

    private IEnumerator PushBack()
    {
        // Assume a constant backward direction
        Vector3 hitDirection = transform.forward;

        // Normalize the hit direction to ensure consistent force regardless of the distance
        hitDirection.Normalize();

        // Store the initial position of the object
        Vector3 initialPosition = transform.position;

        // Calculate the target position based on the hit direction
        Vector3 targetPosition = initialPosition - hitDirection * pushBackForce;

        // Move the object towards the target position over the specified duration
        float elapsedTime = 0f;
        while (elapsedTime < pushBackDuration)
        {
            transform.position = Vector3.Lerp(initialPosition, targetPosition, elapsedTime / pushBackDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure that the object is exactly at the target position
        transform.position = targetPosition;
        
        Debug.Log("ResetSK should be true" + resetSK);
        
    }
    public void ResertingState()
    {
        resetSK = true;
    }

    
}

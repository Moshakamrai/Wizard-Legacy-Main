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
    #endregion

    private void Start()
    {
        enemyHealth = 100;
        proximityThreshold = 3f;
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
        

    }
    internal bool IsAnimationStatePlaying(string stateName)
    {
        // Get the current state information from the Animator
        AnimatorStateInfo stateInfo = animSK.GetCurrentAnimatorStateInfo(0);

        // Check if the given state name matches the name of the currently playing state
        return stateInfo.IsName(stateName);
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
        ParticleManager.Instance.PlayParticle("FirstProjectileExplosion", other.transform.position, transform.rotation, gameObject.transform);
        other.SetActive(false);
        SpellDamageControl(other);
    }

    public void SpellDamageControl(GameObject spellInfo)
    {
        int damageStat = spellInfo.GetComponent<SpellDataRetrieve>().damage;
        enemyHealth -= damageStat;
        hitCounter += 1; 

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

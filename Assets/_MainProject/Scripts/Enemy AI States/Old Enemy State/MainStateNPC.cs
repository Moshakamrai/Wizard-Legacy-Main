using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
public class MainStateNPC : MonoBehaviour
{
    StateMachineNPC currentState;
    public WalkToBuy walking2Market = new WalkToBuy();
    public BuyFruits buyingFruits = new BuyFruits();
    public WalkToExit walking2Exit = new WalkToExit();


    #region Attributes
    internal float stoppingDistance = 0.1f;
    public bool happy;
    [SerializeField] internal bool waitToBuy;
    [SerializeField] public bool showEmote;
    #endregion

    #region Components
    [SerializeField] internal NavMeshAgent navMeshAgent;
    [SerializeField] internal GameObject marketPosition;
    [SerializeField] internal GameObject market;
    [SerializeField] internal GameObject spawn;
    [SerializeField] internal Animator animator;
    [SerializeField] internal Sprite[] moodSpites;
    [SerializeField] internal GameObject backgroundImage;
    #endregion

    private void Start()
    {
        happy = true;
        navMeshAgent = GetComponent<NavMeshAgent>();
        marketPosition = GameObject.FindGameObjectWithTag("MarketPosition");
        market = GameObject.FindGameObjectWithTag("Market");
        spawn = GameObject.FindGameObjectWithTag("Spawn");
        animator = GetComponent<Animator>();
        currentState = walking2Market;
        currentState.EnterState(this);

    }
    public void SwitchState(StateMachineNPC state)
    {
        currentState = state;
        state.EnterState(this);
    }

    internal void Death()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
       
        currentState.UpdateState(this);

        
       
    }
    private void OnTriggerStay(Collider other)
    {
        


    }

    public void SetMood(string moodType)
    {
        backgroundImage.SetActive(true);
        if (moodType == "Happy")
        {
            Image imageComponent = backgroundImage.GetComponent<Image>();
            //spriteRenderer.sprite = moodSpites[0];
            imageComponent.sprite = moodSpites[0];
        }
        else
        {
            Image imageComponent = backgroundImage.GetComponent<Image>();
            imageComponent.sprite = moodSpites[1];
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

    public void LookAtTarget(GameObject target)
    {
        if (target != null)
        {
            // Get the direction from the current position to the target position
            Vector3 direction = target.transform.position - transform.position;

            // Use LookAt to rotate the current GameObject to face the target
            transform.LookAt(target.transform.position);

            // If you want to restrict rotation to only the Y axis (upwards), you can uncomment the following line:
            // transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        }
        else
        {
            Debug.LogError("Target GameObject is null.");
        }
    }
    public IEnumerator WaitToBuy()
    {
        Debug.Log("coroutineA created");
        yield return new WaitForSeconds(2f);
        waitToBuy = true;
        
    }

}

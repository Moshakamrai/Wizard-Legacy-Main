using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerControls : MonoBehaviour
{
    // Singleton instance
    private static PlayerControls instance;

    // Public property to access the singleton instance
    public static PlayerControls Instance
    {
        get
        {
            if (instance == null)
            {
                // If the instance is null, try to find it in the scene
                instance = FindObjectOfType<PlayerControls>();

                // If no instance is found, create a new GameObject and add the script to it
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("PlayerControlsSingleton");
                    instance = singletonObject.AddComponent<PlayerControls>();
                }
            }
            return instance;
        }
    }

    [SerializeField]
    private NavMeshAgent agentPlayer;

    public bool checkPointReached;

    // Prevent instantiation outside the class
    private PlayerControls() { }

    private void Awake()
    {
        // Ensure there's only one instance of the singleton
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

    private void Update()
    {
        if (agentPlayer != null && agentPlayer.remainingDistance < 0.1f)
        {
            // Object has reached the destination
            StopObject();
        }
    }
    public void MoveToCheckPoint(Transform nextCheckPoint)
    {
        if (agentPlayer != null)
        {
            SetDestination(nextCheckPoint);
        }
    }

    private void SetDestination(Transform targetPosition)
    {
        Debug.Log("how many times this getting called 2");
        checkPointReached = false;
        if (agentPlayer != null)
        {
            agentPlayer.isStopped = false;
            agentPlayer.SetDestination(targetPosition.position);
        }
        else
        {
            Debug.LogError("NavMeshAgent component not found.");
        }
    }
 

    private void StopObject()
    {
        agentPlayer.isStopped = true;
        checkPointReached = true;
        // Additional actions when the object reaches the destination
    }
}

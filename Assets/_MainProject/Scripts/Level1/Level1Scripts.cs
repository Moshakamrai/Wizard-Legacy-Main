using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level1Scripts : MonoBehaviour
{
    // Enum for checkpoints
    public enum Checkpoint
    {
        Checkpoint1,
        Checkpoint2,
        EndPoint
    }

    [SerializeField] private GameObject[] enemyGameObjects;
    [SerializeField] private Transform[] checkPoints;
    [SerializeField] private int enemyCount;
    

    void Start()
    {
        // Example usage of the updated method
       
    }

    private void Update()
    {
        MoveEnemiesToCheckpoint(Checkpoint.Checkpoint1);
    }

    // Method to move enemies to a specified checkpoint
    void MoveEnemiesToCheckpoint(Checkpoint checkpoint)
    {
        switch (checkpoint)
        {
            case Checkpoint.Checkpoint1:
                Debug.Log("how many times this getting called 1");
                PlayerControls.Instance.MoveToCheckPoint(checkPoints[0]);
                if (PlayerControls.Instance.checkPointReached)
                {
                    enemyGameObjects[0].gameObject.SetActive(true);
                    enemyGameObjects[1].gameObject.SetActive(true);
                    Debug.Log("how many times this getting called");
                    break;
                }
                if (enemyCount == enemyGameObjects.Length - 2)
                {
                    checkpoint = Checkpoint.Checkpoint2;
                }
                break;

            case Checkpoint.Checkpoint2:
                PlayerControls.Instance.MoveToCheckPoint(checkPoints[1]);
                if (PlayerControls.Instance.checkPointReached)
                {
                    enemyGameObjects[2].gameObject.SetActive(true);
                    enemyGameObjects[3].gameObject.SetActive(true);
                    enemyGameObjects[4].gameObject.SetActive(true);
                }
                if (enemyCount == 0)
                {
                    checkpoint = Checkpoint.EndPoint;
                }
                break;
            case Checkpoint.EndPoint:
                PlayerControls.Instance.MoveToCheckPoint(checkPoints[2]);
                break;
            default:
                Debug.LogError("Invalid checkpoint specified.");
                break;
        }
    }

    
}

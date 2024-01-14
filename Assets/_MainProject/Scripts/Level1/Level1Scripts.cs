using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level1Scripts : MonoBehaviour
{
    // Enum for checkpoints
    public enum Checkpoint
    {
        Checkpoint1,
        Checkpoint2
    }

    [SerializeField] private GameObject[] enemyGameObjects;
    [SerializeField] private Transform[] checkPoints;
    [SerializeField] private int enemyCount;

    void Start()
    {
        // Example usage of the updated method
        MoveEnemiesToCheckpoint(Checkpoint.Checkpoint1);
    }

    // Method to move enemies to a specified checkpoint
    void MoveEnemiesToCheckpoint(Checkpoint checkpoint)
    {
        switch (checkpoint)
        {
            case Checkpoint.Checkpoint1:

                if (enemyCount == enemyGameObjects.Length - 2)
                {

                }
                break;

            case Checkpoint.Checkpoint2:
                // Logic for moving enemies to Checkpoint 2
                break;

            default:
                Debug.LogError("Invalid checkpoint specified.");
                break;
        }
    }
}

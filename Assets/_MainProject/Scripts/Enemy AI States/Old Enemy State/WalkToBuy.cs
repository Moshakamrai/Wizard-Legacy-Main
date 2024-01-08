using UnityEngine;

public class WalkToBuy : StateMachineNPC
{
    public override void EnterState(MainStateNPC enemy1)
    {
        if (enemy1.navMeshAgent != null)
        {
            // Set the destination for the NavMeshAgent
            enemy1.SetDestination(enemy1.marketPosition.transform.position);
        }
        else
        {
            Debug.LogError("NavMeshAgent component not found.");
        }
    }

    public override void UpdateState(MainStateNPC enemy1)
    {
        if (enemy1.navMeshAgent != null && enemy1.navMeshAgent.remainingDistance <= enemy1.stoppingDistance)
        {
            // Stop the NavMeshAgent
            
            enemy1.SwitchState(enemy1.buyingFruits);
        }
    }

    public override void OnCollisionEnter(MainStateNPC enemy1)
    {

    }
}

using UnityEngine;

public class WalkToExit : StateMachineNPC
{
    public override void EnterState(MainStateNPC enemy1)
    {
        enemy1.showEmote = true;
        Debug.Log("tring to get the hell out of here");
        enemy1.animator.SetTrigger("Walk");
        if (enemy1.navMeshAgent != null)
        {
            // Set the destination for the NavMeshAgent

            enemy1.SetDestination(enemy1.spawn.transform.position);
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
           
            enemy1.Death();
        }
    }

    public override void OnCollisionEnter(MainStateNPC enemy1)
    {

    }
}

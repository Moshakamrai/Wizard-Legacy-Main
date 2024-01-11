using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WalkTowardSK : SkeletonStates
{
    public override void EnterState(SkeletonStateMachine enemy1)
    {
        
        //enemy1.resetSK = false;
        Debug.Log("made it false");
        enemy1.animSK.SetTrigger("Walk");
       // Debug.Log("Back to walking");
        if (enemy1.navMeshAgent != null)
        {
            // Set the destination for the NavMeshAgent
            enemy1.SetDestination(enemy1.mainPlayer.transform.position);
            enemy1.navMeshAgent.isStopped = false;
        }
        else
        {
            Debug.LogError("NavMeshAgent component not found.");
        }
    }
    public override void UpdateState(SkeletonStateMachine enemy1)
    {
        if (enemy1.hitCounter % 3 == 0 && enemy1.hitCounter != 0)
        {
            enemy1.hitCounter = 0;
            enemy1.navMeshAgent.isStopped = true;
            enemy1.SwitchState(enemy1.getHurt);
            enemy1.ApplyPushBack();
        }
        Debug.Log("in walking state");
        if (Vector3.Distance(enemy1.transform.position, enemy1.mainPlayer.transform.position) < enemy1.proximityThreshold)
        {
            enemy1.navMeshAgent.isStopped = true;
            enemy1.SwitchState(enemy1.attack);
        }
    }
    public override void OnCollisionEnter(SkeletonStateMachine enemy1)
    {

    }
    

}

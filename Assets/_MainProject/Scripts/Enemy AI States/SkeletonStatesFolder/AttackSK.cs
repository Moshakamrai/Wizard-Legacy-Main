using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSK : SkeletonStates
{
    public override void EnterState(SkeletonStateMachine enemySK)
    {
        Debug.Log("going to attuck");
        enemySK.animSK.SetTrigger("AttackSK");
    }
    public override void UpdateState(SkeletonStateMachine enemy1)
    {
        if (Vector3.Distance(enemy1.transform.position, enemy1.mainPlayer.transform.position) > enemy1.proximityThreshold)
        {
            
            //enemy1.navMeshAgent.isStopped = false;
            enemy1.SwitchState(enemy1.walktoward);
        }
        Debug.Log("in attacking state");
    }
    public override void OnCollisionEnter(SkeletonStateMachine enemySK)
    {

    }
}

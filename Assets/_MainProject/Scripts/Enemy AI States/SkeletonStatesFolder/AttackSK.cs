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
        //if (Vector3.Distance(enemy1.transform.position, enemy1.mainPlayer.transform.position) > enemy1.proximityThreshold)
        //{
        //    enemy1.SwitchState(enemy1.walktoward);
        //}
        if (enemy1.resetSK)
        {
            enemy1.SwitchState(enemy1.walktoward);
        }
        if (enemy1.hitCounter % 3 == 0 && enemy1.hitCounter != 0)
        {
            enemy1.hitCounter = 0;
            enemy1.navMeshAgent.isStopped = true;
            enemy1.SwitchState(enemy1.getHurt);
            enemy1.ApplyPushBack();
        }
    }
    public override void OnCollisionEnter(SkeletonStateMachine enemySK)
    {

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetHurtSK : SkeletonStates
{
    public override void EnterState(SkeletonStateMachine enemySK)
    {
        enemySK.ApplyPushBack();
        enemySK.animSK.SetTrigger("GetHurtSK");
        //Debug.Log("should get hurt now");
        enemySK.navMeshAgent.isStopped = true;

    }
    public override void UpdateState(SkeletonStateMachine enemySK)
    {
        //Debug.Log("in hruting state");
        if (enemySK.resetSK)
        {
            enemySK.SwitchState(enemySK.walktoward);
        }
    }
    public override void OnCollisionEnter(SkeletonStateMachine enemySK)
    {

    }
}

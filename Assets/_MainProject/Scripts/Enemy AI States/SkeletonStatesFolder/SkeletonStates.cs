using UnityEngine;

public abstract class SkeletonStates
{
    public abstract void EnterState(SkeletonStateMachine skeletonNPC);

    public abstract void UpdateState(SkeletonStateMachine skeletonNPC);

    public abstract void OnCollisionEnter(SkeletonStateMachine skeletonNPC);
}

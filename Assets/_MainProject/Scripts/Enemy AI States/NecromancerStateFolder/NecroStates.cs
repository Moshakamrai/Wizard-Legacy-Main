using UnityEngine;
public abstract class NecroStates 
{
    public abstract void EnterState(NecroStateMachine skeletonNPC);

    public abstract void UpdateState(NecroStateMachine skeletonNPC);

    public abstract void OnCollisionEnter(NecroStateMachine skeletonNPC);
}

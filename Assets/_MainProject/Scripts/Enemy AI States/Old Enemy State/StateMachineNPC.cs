using UnityEngine;

public abstract class StateMachineNPC
{
    public abstract void EnterState(MainStateNPC firstNPC);

    public abstract void UpdateState(MainStateNPC firstNPC);

    public abstract void OnCollisionEnter(MainStateNPC firstNPC);
}

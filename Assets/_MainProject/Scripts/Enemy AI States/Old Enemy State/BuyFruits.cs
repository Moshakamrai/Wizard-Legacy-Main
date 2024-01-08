using UnityEngine;

public class BuyFruits : StateMachineNPC
{
    public override void EnterState(MainStateNPC enemy1)
    {
        enemy1.animator.SetTrigger("Buy");
        enemy1.LookAtTarget(enemy1.market);
        enemy1.StartCoroutine(enemy1.WaitToBuy());
    }

    public override void UpdateState(MainStateNPC enemy1)
    {
        if (enemy1.waitToBuy)
        {
            enemy1.SwitchState(enemy1.walking2Exit);
        }
    }

    public override void OnCollisionEnter(MainStateNPC enemy1)
    {

    }
}

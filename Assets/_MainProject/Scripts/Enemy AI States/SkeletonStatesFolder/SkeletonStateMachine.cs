using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class SkeletonStateMachine : MonoBehaviour
{
    SkeletonStates currentState;
    internal WalkTowardSK walktoward = new WalkTowardSK();
    internal GetHurtSK getHurt = new GetHurtSK();
    internal AttackSK attack = new AttackSK();

    #region Attributes
    #endregion

    #region Components
    #endregion

    private void Start()
    {
        currentState.EnterState(this);
    }
    public void SwitchState(SkeletonStates state)
    {
        currentState = state;
        state.EnterState(this);
    }

    private void Update()
    {
        currentState.UpdateState(this);
    }


}

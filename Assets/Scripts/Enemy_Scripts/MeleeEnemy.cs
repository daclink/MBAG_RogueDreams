using System;
using Enemy_Scripts;
using UnityEngine;

public class MeleeEnemy : BaseEnemy
{

    protected override void PostStart()
    {
        health = 10f;
        attackDmg = 2f;
        moveSpeed = 5f;
        aggroRange = 3f;

    }

    protected override void Update()
    {
        base.Update();
        
        //other update code here
        
    }

    protected override void HandleIdle()
    {
        throw new NotImplementedException();
    }

    protected override void HandleAgro()
    {
        throw new NotImplementedException();
    }

    protected override void HandlePatrol()
    {
        throw new NotImplementedException();
    }

    protected override void HandleAttacking()
    {
        throw new NotImplementedException();
    }

    protected override void HandleDamage()
    {
        //TODO: change dmgAmount to the player attack damage
        base.TakeDamage(10);
    }

    protected override void HandleDead()
    {
        throw new NotImplementedException();
    }
    
    
}

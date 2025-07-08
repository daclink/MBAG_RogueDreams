namespace Enemy_Scripts
{
    
    
    /**
     * Idle is for when the enemy is offscreen
     * TakeDamage is for when the enemy takes damage
     * Agro is for when the player is within agroRange
     * Dead is for when the enemy is dead and health goes below 0
     * Patrol is for when the enemy is onscreen or within a distance to start patrolling
     *      - this might be random movements to simulate walking around
     * Attacking is for when the player is within the range of attack
     */
    public enum EnemyState
    {
        Idle,
        TakeDamage,
        Agro,
        Dead,
        Patrol,
        Attacking
    }
}
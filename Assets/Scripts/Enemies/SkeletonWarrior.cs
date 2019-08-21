using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonWarrior : Creatures
{
    Vector2 distance;

    float xVelocity = 0;

    public float attackTime;
    public float waitingTime = 3f;              //Time to waiting before going
    float waitTime = 0;
    public float dyingTime = 1.3f;

    public float searchingTime = 5f;
    float searchTime = 0;

    public float flippingTime = 1f;             //Time to flip character while searching
    float flipTime = 0;

    public bool isChasing = false;
    public bool isSearching = false;

    public float attackDistance = 2f;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();

        //Record the original x scale of the player
        originalXScale = transform.localScale.x;

        isCharacterDirectionFlipped = false;
        isDead = false;

        animEnemyHit = Animator.StringToHash("Hurt");

        zoneOfVision = GetComponentInChildren<PolygonCollider2D>();

    }

    // Update is called once per frame
    void Update()
    {
        PhysicsCheck();

        if (isChasing || isSearching)
            Chase();
        else
            Move();

        DinamicZOV();

        if (healthSystem.GetHealth() == 0 && !isDead)
        {
            isDead = true;
            isChasing = false;
            isSearching = false;
            deadBody = Resources.Load<GameObject>("DeadCreatures/deadSkeletonWarrior");
            dieTime = Time.time + dyingTime;
        }

        Die(isDead);
        if (isHit)
            animator.Play(animEnemyHit);
        isHit = false;
        animator.SetInteger("health", healthSystem.GetHealth());
        //Debug.Log(healthSystem.GetHealth());
    }

    private void Move()
    {
        if (isDead)
            return;

        rigidBody.velocity = new Vector2(xVelocity, rigidBody.velocity.y);
        if (Time.time < waitTime)
            xVelocity = 0f;
        else
            xVelocity = speed * direction;
        if (!wallCheck.collider)
        {
            // The player is noticed - the beginning of the pursuit
            if (zoneOfVision.IsTouchingLayers(playerMask))
                isChasing = true;
            
            if (!groundCheck.collider)
            {
                FlipCharacterDirection();
                zoneOfVision.transform.localScale = new Vector3(1, 1, 1);
                waitTime = Time.time + waitingTime;
            }
        }
        else
        {
            FlipCharacterDirection();
            zoneOfVision.transform.localScale = new Vector3(1, 1, 1);
            waitTime = Time.time + waitingTime;
        }
        animator.SetInteger("speed", (int)xVelocity);
    }

    void SearchPlayer()
    {
        // Beginning of the search if the player has lost sight
        if (!zoneOfVision.IsTouchingLayers(playerMask) && isChasing)
        {
            isAttacking = false;
            isSearching = true;
            isChasing = false;
            searchTime = Time.time + searchingTime;
            flipTime = Time.time + flippingTime;
        }

        // Periodic character rotation for player search
        if (isSearching && searchTime > Time.time)
        {
            if (zoneOfVision.IsTouchingLayers(playerMask) && !zoneOfVision.IsTouchingLayers(groundMask))
            {
                isChasing = true;
                isSearching = false;
            }

            xVelocity = 0;
            if (Time.time > flipTime)
            {
                FlipCharacterDirection();
                zoneOfVision.transform.localScale = new Vector3(1, 1, 1);
                flipTime = Time.time + flippingTime;
            }
        }
        else
            isSearching = false;
    }

    private void Chase()
    {
        if (!player)
            return;

        xVelocity = speed * direction;
        rigidBody.velocity = new Vector2(xVelocity, rigidBody.velocity.y);
        distance = transform.position - player.transform.position;
        // Stop chasing if there is an abyss in front of the character
        if (!groundCheck.collider && isChasing)
        {
            FlipCharacterDirection();
            isChasing = false;
            // Returning the field of view to its original size
            zoneOfVision.transform.localScale = new Vector3(1, 1, 1);
            waitTime = Time.time + waitingTime;
        }
        // Checking the distance between the player and the character, to carry out an attack by the character
        if (Math.Abs(distance.x) - attackDistance > -2f && Math.Abs(distance.x) - attackDistance < 0.15f)
        {
            attackTime = Time.time + waitingTime / 2f;
            xVelocity = 0;
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
        }
        if (Time.time < attackTime)
            xVelocity = 0;

        SearchPlayer();
        
        //Debug.Log("Time: " + Time.time + "; SearchTime: " + searchTime + "; FlipTime: " + flipTime);
        rigidBody.velocity = new Vector2(xVelocity, rigidBody.velocity.y);
        animator.SetInteger("speed", (int)xVelocity);
        animator.SetBool("isAttacking", isAttacking);
    }

    private void Attack()
    {
        Action(attackDetection.position, attackRadius, 31, false);
    }

    protected override void OnTriggerEnter2D(Collider2D collider)
    {
        Projectile projectile = collider.gameObject.GetComponent<Projectile>();
        if (projectile && gameObject != projectile.Parent)
        {
            healthSystem.Damage(30);
            isHit = true;
            if (!isChasing)
            {
                isSearching = true;
                searchTime = Time.time + searchingTime;
                flipTime = Time.time + flippingTime;
            }
        }
    }
}

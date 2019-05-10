using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonWarrior : Creatures
{
    PolygonCollider2D zoneOfVision;

    public float vision = 5f;
    
    Vector2 distance;

    float distanceGroundOriginal;
    float xVelocity = 0;

    public float attackTime;
    public float waitingTime = 3f;              //Time to waiting before going
    float waitTime = 0;
    public float dyingTime = 1.3f;
    float dieTime;

    public float searchingTime = 5f;
    float searchTime = 0;

    public float flippingTime = 1f;
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

        distanceGroundOriginal = distanceGround;

        //Record the original x scale of the player
        originalXScale = transform.localScale.x;

        //Record initial collider size and offset
        colliderStandSize = bodyCollider.size;
        colliderStandOffset = bodyCollider.offset;

        //Calculate crouching collider size and offset
        colliderCrouchSize = new Vector2(bodyCollider.size.x, bodyCollider.size.y / 1.4f);
        colliderCrouchOffset = new Vector2(bodyCollider.offset.x, bodyCollider.offset.y / 1.3f);

        isCharacterDirectionFlipped = false;

        animEnemyHit = Animator.StringToHash("Hurt");

        zoneOfVision = GetComponentInChildren<PolygonCollider2D>();
        groundMask = LayerMask.GetMask("Ground");
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        //Changing ZoneOfVision
        if (zoneOfVision.IsTouchingLayers(groundMask))
        {
            float xTouchPoint = transform.position.x;
            if (zoneOfVision.transform.localScale.x > 0)
                zoneOfVision.transform.localScale = new Vector3(zoneOfVision.transform.localScale.x - 0.1f, 1, 1);
            else
                zoneOfVision.transform.localScale = new Vector3(zoneOfVision.transform.localScale.x + 0.1f, 1, 1);
        }

        if (healthSystem.GetHealth() == 0 && !isDead)
        {
            isDead = true;
            dieTime = Time.time + dyingTime;
        }
        //Death Checking
        if (isDead)
        {
            isHit = false;
            isChasing = false;
            isSearching = false;
            GameObject deadSkeletonWarrior = Resources.Load<GameObject>("DeadCreatures/DeadSkeletonWarrior");
            Vector3 position = transform.position;
            Vector3 scale = transform.localScale;
            rigidBody.velocity = new Vector2(0, rigidBody.velocity.y);
            deadSkeletonWarrior.transform.position = position;
            deadSkeletonWarrior.transform.localScale = scale;
            if (dieTime < Time.time)
            {
                Destroy(gameObject);
                Instantiate(deadSkeletonWarrior);
            }
        }
        if (isHit)
            animator.Play(animEnemyHit);
        isHit = false;

        animator.SetInteger("health", healthSystem.GetHealth());
        //Debug.Log(healthSystem.GetHealth());
    }

    private void Move()
    {
        if (!isDead)
        {
            rigidBody.velocity = new Vector2(xVelocity, rigidBody.velocity.y);
            groundInfo = Physics2D.Raycast(groundDetection.position, Vector2.down, distanceGround, bgMask);
            Debug.DrawRay(groundDetection.position, Vector2.down * distanceGround);
            if (waitTime > Time.time)
                xVelocity = 0f;
            else
                xVelocity = speed * direction;
            if (!groundInfo.collider || groundInfo.collider.isTrigger)
            {
                groundInfo = Physics2D.Raycast(new Vector2(groundDetection.position.x, groundDetection.position.y - distanceGround), Vector2.down, distanceGround / 3, bgMask);
                Chase();
                Debug.DrawRay(new Vector2(groundDetection.position.x, groundDetection.position.y - distanceGround), Vector2.down * distanceGround / 3, Color.red);
                if (!groundInfo.collider && isChasing)
                {
                    FlipCharacterDirection();
                    isChasing = false;
                    zoneOfVision.transform.localScale = new Vector3(1, 1, 1);
                    waitTime = Time.time + waitingTime;
                }
                else if (!groundInfo.collider)
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
            distanceGround = distanceGroundOriginal;
            animator.SetInteger("speed", (int)xVelocity);
        }
        
    }

    private void Chase()
    {
        if (zoneOfVision.IsTouchingLayers(playerMask))
        {
            isSearching = false;
            isChasing = true;
            xVelocity = speed * direction;
            distance = transform.position - player.transform.position;
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
            if (attackTime > Time.time)
                xVelocity = 0;
        }
        else if (!zoneOfVision.IsTouchingLayers(playerMask) && isChasing)
        {
            isAttacking = false;
            isSearching = true;

            searchTime = Time.time + searchingTime;
            flipTime = Time.time;
            isChasing = false;
        }

        if (isSearching && searchTime > Time.time)
        {
            xVelocity = 0;
            if (flipTime < Time.time)
            {
                FlipCharacterDirection();
                zoneOfVision.transform.localScale = new Vector3(1, 1, 1);
                flipTime = Time.time + flippingTime;
            }
        }
        else
            isSearching = false;
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
                flipTime = Time.time;
            }
        }
    }
}

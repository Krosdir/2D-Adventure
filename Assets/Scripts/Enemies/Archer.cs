using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer : Creatures
{
    float xVelocity = 0;

    public float attackTime;
    public float waitingTime = 3f;              //Time to waiting before going
    float waitTime = 0;
    public float dyingTime = 1f;

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

        animEnemyHit = Animator.StringToHash("Hit");

        zoneOfVision = GetComponentInChildren<PolygonCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        PhysicsCheck();
        Move();
        DinamicZOV();

        if (healthSystem.GetHealth() == 0 && !isDead)
        {
            isDead = true;
            deadBody = Resources.Load<GameObject>("DeadCreatures/deadArcher");
            dieTime = Time.time + dyingTime;
        }

        Die(isDead);
        if (isHit)
            animator.Play(animEnemyHit);
        isHit = false;
        animator.SetInteger("health", healthSystem.GetHealth());
        //Debug.Log(healthSystem.GetHealth());
    }

    protected override void Shoot()
    {
        Vector3 position = transform.position;
        position.x = (isCharacterDirectionFlipped ? position.x += -1f : position.x += 1f);
        position.y += 1.3f;   //Projectile release point relative to the character
        NewProjectile = Instantiate(projectile, position, projectile.transform.rotation) as Projectile;
        NewProjectile.Parent = gameObject;
        NewProjectile.Direction = NewProjectile.transform.right * (isCharacterDirectionFlipped ? -1.0f : 1.0f);
        IgnoreEnemyZOV();
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
            {
                xVelocity = 0;
                isAttacking = true;
            }
            else
                isAttacking = false;

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
        animator.SetBool("isAttacking", isAttacking);
    }

    protected override void OnTriggerEnter2D(Collider2D collider)
    {
        Projectile projectile = collider.gameObject.GetComponent<Projectile>();
        if (projectile && gameObject != projectile.Parent)
        {
            healthSystem.Damage(50);
            isHit = true;
            //TODO: Behavior after being hit
            //if (!isChasing)
            //{
            //    isSearching = true;
            //    searchTime = Time.time + searchingTime;
            //    flipTime = Time.time + flippingTime;
            //}
        }
    }
}

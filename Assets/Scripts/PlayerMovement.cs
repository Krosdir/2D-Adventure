// This script controls the player's movement and physics within the game

using UnityEngine;

public class PlayerMovement : Creatures
{
    [SerializeField] private ParticleSystem dustPS;
    [SerializeField] private Transform airAttackDetection;
    [SerializeField] private float airAttackRadius;

    [Header("Movement Properties")]
    public float crouchSpeedDivisor = 3f;   //Speed reduction when crouching
    public float slideSpeedDivesor = 3f;
    public float coyoteDuration = .05f;     //How long the player can jump after falling
    public float attackDuration = .3f;     //How long the player can continue attacking after previous attack
    public float maxFallSpeed = -25f;       //Max speed player can fall

    [Header("Jump Properties")]
    public float jumpForce = 6.3f;          //Initial force of jump
    public float hangingJumpForce = 15f;    //Force of wall hanging jumo
    public float slidingJumpForce = 4f;

    [Header("Environment Check Properties")]
    public float footOffset = .4f;          //X Offset of feet raycast
    public float eyeHeight = 1.5f;          //Height of wall checks
    public float reachOffset = .7f;         //X offset for wall grabbing
    public float headClearance = .5f;       //Space needed above the player's head
    public float groundDistance = .2f;      //Distance player is considered to be on the ground
    public float grabDistance = .4f;        //The reach distance for wall grabs

    [Header("Status Flags")]
    public bool isOnGround;                 //Is the player on the ground?
    public bool isJumping;                  //Is player jumping?
    public bool isHanging;                  //Is player hanging?
    public bool isCrouching;
    
    public bool isSliding;
    public bool canStand;

    PlayerInput input;                      //The current inputs for the player

    float jumpTime;                         //Variable to hold jump duration
    float coyoteTime;                       //Variable to hold coyote duration
    float attackTime;
    float playerHeight;                     //Height of the player

    Vector2 colliderCrouchSize;             //Size of the crouching collider
    Vector2 colliderCrouchOffset;           //Offset of the crouching collider

    const float smallAmount = .05f;         //A small amount used for hanging position

    float xVelocity = 0;
    float yVelocity = 0;

    private PlayerCombo attack = new PlayerCombo(new string[] { "attack" });
    private PlayerCombo doubleAttack = new PlayerCombo(new string[] { "attack", "attack" });
    private PlayerCombo trippleAttack = new PlayerCombo(new string[] { "attack", "attack", "attack" });
    private PlayerCombo airAttack = new PlayerCombo(new string[] { "attack" });
    private PlayerCombo doubleAirAttack = new PlayerCombo(new string[] { "attack", "attack" });
    private PlayerCombo trippleAirAttack = new PlayerCombo(new string[] { "attack", "attack", "attack" });
    private PlayerCombo shoot = new PlayerCombo(new string[] { "shoot" });
    private PlayerCombo airShoot = new PlayerCombo(new string[] { "shoot" });

    readonly int animAttack1 = Animator.StringToHash("FirstAttack");
    readonly int animAttack2 = Animator.StringToHash("SecondAttack");
    readonly int animAttack3 = Animator.StringToHash("ThirdAttack");

    readonly int animAirAttack1 = Animator.StringToHash("FirstAirAttack");
    readonly int animAirAttack2 = Animator.StringToHash("SecondAirAttack");
    readonly int animAirAttack3 = Animator.StringToHash("ThirdAirAttackBegin");

    readonly int animShoot = Animator.StringToHash("Shoot");
    readonly int animAirShoot = Animator.StringToHash("AirShoot");

    readonly int animPlayerHit = Animator.StringToHash("Hit");
    readonly int animPlayerDying = Animator.StringToHash("Dying");

    GameObject deathEffect;

    void Start()
    {
        projectile = Resources.Load<Projectile>("Arrow");

        //Get a reference to the required components
        input = GetComponent<PlayerInput>();
        rigidBody = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();

        deathEffect = Resources.Load<GameObject>("DeathEffect");

        //Record the original x scale of the player
        originalXScale = transform.localScale.x;

        //Record the player's height from the collider
        playerHeight = bodyCollider.size.y;

        //Record initial collider size and offset
        colliderStandSize = bodyCollider.size;
        colliderStandOffset = bodyCollider.offset;

        //Calculate crouching collider size and offset
        colliderCrouchSize = new Vector2(bodyCollider.size.x, bodyCollider.size.y / 1.4f);
        colliderCrouchOffset = new Vector2(bodyCollider.offset.x, bodyCollider.offset.y / 1.3f);

        isCharacterDirectionFlipped = false;
        isDead = false;
    }

    void FixedUpdate()
    {
        if (healthSystem.GetHealth() == 0 && !isDead)
        {
            isDead = true;
            animator.Play(animPlayerDying);
            rigidBody.bodyType = RigidbodyType2D.Static;
            //Death effect in front of player
            Vector3 deathEffectPos = new Vector3(transform.position.x, transform.position.y + 1, -2);
            Destroy(Instantiate(deathEffect, deathEffectPos, transform.rotation), 0.8f);
            Destroy(gameObject, 0.7f);
        }
        
        if (isHit)
            animator.Play(animPlayerHit);
        if (isDead)
            isHit = false;
        else
        {
            //Check the environment to determine status
            PhysicsCheck();

            //Process ground and air movements
            GroundMovement();
            MidAirMovement();
        }

        isHit = false;
        animator.SetInteger("health", healthSystem.GetHealth());
    }

    private void Update()
    {
        
    }

    protected override void PhysicsCheck()
    {
        //Start by assuming the player isn't on the ground and the head isn't blocked
        isOnGround = false;
        rigidBody.bodyType = RigidbodyType2D.Dynamic;
        //Cast rays for the left and right foot and check above the player's head
        RaycastHit2D leftCheck = Raycast(new Vector2(-footOffset, colliderStandOffset.y * colliderStandSize.y), Vector2.down, groundDistance);
        RaycastHit2D rightCheck = Raycast(new Vector2(footOffset, colliderStandOffset.y * colliderStandSize.y), Vector2.down, groundDistance);
        RaycastHit2D headCheck = Raycast(new Vector2(0f, bodyCollider.size.y), Vector2.up, headClearance);
        Vector2 grabDir = new Vector2(direction, 0f);
        wallCheck = Raycast(new Vector2(footOffset * direction, eyeHeight), grabDir, grabDistance);

        //If either ray hit the ground, the player is on the ground
        if (leftCheck || rightCheck)
        {
            isOnGround = true;
            canStand = true;
            input.attackButton.enabled = true;
            input.shootButton.enabled = true;
        }

        if (isOnGround && isCrouching && headCheck && input.verticalMove > -.5f)
            canStand = false;
        else
            canStand = true;

        animator.SetBool("isOnGround", isOnGround);

        if (!isOnGround && rigidBody.velocity.y < 0f && wallCheck)
            isSliding = true;
    }

    void GroundFight()
    {
        if (input.attackPressed && isOnGround)
        {
            attackTime = Time.time + attackDuration;
            if (attackTime > Time.time)
                isAttacking = true;
            if (trippleAttack.Check())
                animator.Play(animAttack3);
            else if (doubleAttack.Check())
                animator.Play(animAttack2);
            else if (attack.Check())
                animator.Play(animAttack1);
        }
        else if (input.shootPressed && isOnGround)
        {
            attackTime = Time.time + attackDuration;
            if (attackTime > Time.time)
                isAttacking = true;
            if (shoot.Check())
                animator.Play(animShoot);
        }
        // No attacking, no shooting - isAttacking = false
        else if ((!input.attackPressed && attackTime < Time.time) || (!input.shootPressed && attackTime < Time.time)) 
            isAttacking = false;

        if (isAttacking)
            xVelocity /= crouchSpeedDivisor;
        animator.SetBool("isAttacking", isAttacking);
    }

    void AirFight()
    {
        if (input.attackPressed && !isOnGround)
        {
            attackTime = Time.time + attackDuration / 2;
            if (attackTime > Time.time)
                isAttacking = true;
            if (trippleAirAttack.Check())
            {
                input.attackButton.enabled = false;
                input.shootButton.enabled = false;
                animator.Play(animAirAttack3);
                rigidBody.AddForce(new Vector2(0f, -jumpForce), ForceMode2D.Impulse);
            }
            else if (doubleAirAttack.Check())
                animator.Play(animAirAttack2);
            else if (airAttack.Check())
                animator.Play(animAirAttack1);
        }
        else if (input.shootPressed && !isOnGround)
        {
            attackTime = Time.time + attackDuration;
            if (attackTime > Time.time)
                isAttacking = true;
            if (airShoot.Check())
                animator.Play(animAirShoot);
        }
        // No attacking, no shooting - isAttacking = false
        else if ((!input.attackPressed && attackTime < Time.time) || (!input.shootPressed && attackTime < Time.time)) 
            isAttacking = false;
    }

    void GroundMovement()
    {
        if (isSliding)
            return;
        //Handle crouching input. If holding the crouch button but not crouching, crouch
        if ((input.verticalMove <= -.5f && !isCrouching && isOnGround) || !canStand)
            Crouch();
        //Otherwise, if not holding crouch but currently crouching, stand up
        else if (input.verticalMove > -.5f && isCrouching)
            StandUp();
        //Otherwise, if crouching and no longer on the ground, stand up
        else if (!isOnGround && isCrouching)
            StandUp();

        animator.SetBool("isCrouching", isCrouching);

        //Calculate the desired velocity based on inputs
        xVelocity = 0;
        if (input.horizontalMove >= .25f || input.horizontalMove <= -.25f)
            xVelocity = speed * input.horizontalMove;
        animator.SetInteger("speed", (int)xVelocity);

        //If the sign of the velocity and direction don't match, flip the character
        if (xVelocity * direction < 0f)
            FlipCharacterDirection();

        if (isCrouching)
            xVelocity /= crouchSpeedDivisor;

        //If the player is on the ground, extend the coyote time window
        if (isOnGround)
        {
            coyoteTime = Time.time + coyoteDuration;
        }

        GroundFight();

        rigidBody.velocity = new Vector2(xVelocity, rigidBody.velocity.y);
    }

    void MidAirMovement()
    {
        if (isSliding)
        {
            yVelocity = rigidBody.velocity.y / slideSpeedDivesor;
            xVelocity = 0;
            input.attackButton.enabled = false;
            input.shootButton.enabled = false;
            rigidBody.velocity = new Vector2(xVelocity, yVelocity);

            if (isOnGround)
            {
                isSliding = false;
                return;
            }

            if (isHit)
            {
                isSliding = false;
                isHit = false;
                rigidBody.AddForce(slidingJumpForce * (isCharacterDirectionFlipped ? transform.right : -transform.right), ForceMode2D.Impulse);
                FlipCharacterDirection();
                isJumping = true;
                return;
            }

            //If jump is pressed...
            if (input.jumpPressed)
            {
                //...let go...
                isSliding = false;
                input.attackButton.enabled = true;
                input.shootButton.enabled = true;
                //...set the rigidbody to dynamic and apply a jump force...
                FlipCharacterDirection();
                rigidBody.AddForce(slidingJumpForce * (transform.up + (isCharacterDirectionFlipped ? -transform.right : transform.right)), ForceMode2D.Impulse);
                isJumping = true;
                //...and exit
                return;
            }

        }
        animator.SetBool("isSliding", isSliding);
        //If the jump key is pressed AND the player isn't already jumping AND EITHER
        //the player is on the ground or within the coyote time window...
        if (input.jumpPressed && !isJumping && (isOnGround || coyoteTime > Time.time))
        {

            //...The player is no longer on the groud and is jumping...

            isJumping = true;
            dustPS.transform.position = transform.position;
            dustPS.Play();
            //...add the jump force to the rigidbody...
            rigidBody.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);

            //...and tell the Audio Manager to play the jump audio
            //AudioManager.PlayJumpAudio();
        }
        else if (rigidBody.velocity.y == 0 || isSliding)
            isJumping = false;
        //Otherwise, if currently within the jump time window...
        //else if (isJumping)
        //{
        //    //...and the jump button is held, apply an incremental force to the rigidbody...
        //    //if (input.jumpHeld)
        //    //    rigidBody.AddForce(new Vector2(0f, jumpHoldForce), ForceMode2D.Impulse);

        //    //...and if jump time is past, set isJumping to false
        //    if (jumpTime <= Time.time)
        //        isJumping = false;
        //}

        AirFight();

        //If player is falling to fast, reduce the Y velocity to the max
        if (rigidBody.velocity.y < maxFallSpeed)
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, maxFallSpeed);
        animator.SetBool("isJumping", isJumping);
        animator.SetInteger("speedY", (int)rigidBody.velocity.y);
    }

    void Crouch()
    {
        //The player is crouching
        isCrouching = true;

        //Apply the crouching collider size and offset
        bodyCollider.size = colliderCrouchSize;
        bodyCollider.offset = colliderCrouchOffset;
    }

    void StandUp()
    {
        //The player isn't crouching
        isCrouching = false;
        //Apply the standing collider size and offset
        bodyCollider.size = colliderStandSize;
        bodyCollider.offset = colliderStandOffset;
    } 

    private void Attack()
    {
        Action(attackDetection.position, attackRadius, 30, false);
    }
    private void AirAttack()
    {
        Action(airAttackDetection.position, airAttackRadius, 30, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Projectile projectile = collision.gameObject.GetComponent<Projectile>();
        if (projectile && gameObject != projectile.Parent)
        {
            isHit = true;
            healthSystem.Damage(30);
        }
    }
}


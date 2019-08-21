using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creatures : MonoBehaviour
{
    [Header("Gizmos")]
    [SerializeField] protected PolygonCollider2D zoneOfVision;
    [SerializeField] protected Transform attackDetection;
    [SerializeField] protected float attackRadius;
    bool drawDebugRaycasts = true;   //Should the environment checks be visualized

    protected HealthSystem healthSystem = new HealthSystem(100);
    public Transform resHealthBar;
    HealthBar healthBar;

    [SerializeField] protected Projectile projectile;
    protected Projectile NewProjectile { get; set; }
    [Header("Movement Properties")]
    public float speed = 8f;                //Player speed

    [Header("Status Flags")]
    public bool isCharacterDirectionFlipped;
    public bool isAttacking;
    public bool isHit;
    protected bool isDead;

    protected Vector2 colliderStandSize;              //Size of the standing collider
    protected Vector2 colliderStandOffset;            //Offset of the standing collider

    protected float originalXScale;                   //Original scale on X axis
    protected int direction = 1;                      //Direction player is facing
    protected float dieTime;

    protected BoxCollider2D bodyCollider;             //The collider component
    [HideInInspector] public Rigidbody2D rigidBody;   //The rigidbody component
    protected Animator animator;

    static protected PlayerMovement player;

    protected RaycastHit2D groundCheck;
    protected RaycastHit2D wallCheck;

    protected int animEnemyHit;

    protected LayerMask bgMask;
    protected LayerMask groundMask;
    protected LayerMask playerMask;

    protected GameObject deadBody;

    protected void Awake()
    {
        if (gameObject.GetComponent<PlayerMovement>())
        {
            healthBar = resHealthBar.GetComponent<HealthBar>();
            healthBar.Setup(healthSystem);
        }
        player = FindObjectOfType<PlayerMovement>();
        playerMask = LayerMask.GetMask("Player");
        groundMask = LayerMask.GetMask("Ground");
        bgMask = LayerMask.GetMask("Background");
        bgMask = ~bgMask;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    protected virtual void PhysicsCheck()
    {
        //Cast ray for checking ground ((isCharacterDirectionFlipped ? .6f : -.6f) - Костыль для оцентровки, т.к. пивот стоит не по центру)
        groundCheck = Raycast(new Vector2(isCharacterDirectionFlipped ? -.6f : .6f, colliderStandOffset.y * colliderStandSize.y), Vector2.down, .1f);

        Vector2 grabDir = new Vector2(direction, 0f);

        //Cast ray to look for a wall
        wallCheck = Raycast(new Vector2(.4f * direction, 1.5f), grabDir, .1f);
    }

    protected void DinamicZOV()
    {
        //Changing ZoneOfVision
        if (!zoneOfVision.IsTouchingLayers(groundMask))
            return;

        float xTouchPoint = transform.position.x;
        if (zoneOfVision.transform.localScale.x > 0)
        {
            float smallerZOV = zoneOfVision.transform.localScale.x - 0.1f;
            zoneOfVision.transform.localScale = new Vector3(smallerZOV, 1, 1);
        }
        else
        {
            float biggerZOV = zoneOfVision.transform.localScale.x + 0.1f;
            zoneOfVision.transform.localScale = new Vector3(biggerZOV, 1, 1);
        }
    }

    protected void IgnoreEnemyZOV()
    {
        //Ignoring all enemy zones of vision
        for (int i = 0; i < GameObject.FindGameObjectsWithTag("Enemies").Length; i++)
        {
            NewProjectile.zoneOfVisionOfCreatures.Add(GameObject.FindGameObjectsWithTag("Enemies")[i].GetComponentInChildren<PolygonCollider2D>());
            Physics2D.IgnoreCollision(NewProjectile.GetComponent<BoxCollider2D>(), NewProjectile.zoneOfVisionOfCreatures[i]);
        }
    }

    protected virtual void Shoot()
    {
        Vector3 position = transform.position;
        position.x = (isCharacterDirectionFlipped ? position.x += -1f : position.x += 1f);
        position.y += 0.7f;   //Projectile release point relative to the character
        NewProjectile = Instantiate(projectile, position, projectile.transform.rotation) as Projectile;
        NewProjectile.Parent = gameObject;
        NewProjectile.Direction = NewProjectile.transform.right * (isCharacterDirectionFlipped ? -1.0f : 1.0f);
        IgnoreEnemyZOV();
    }

    protected virtual void FlipCharacterDirection()
    {
        //Turn the character by flipping the direction
        direction *= -1;

        isCharacterDirectionFlipped = !isCharacterDirectionFlipped;

        //Record the current scale
        Vector3 scale = transform.localScale;

        //Set the X scale to be the original times the direction
        scale.x = originalXScale * direction;

        //Apply the new scale
        transform.localScale = scale;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Traps"))
        {
            isHit = true;
            healthSystem.Damage(10);
        }
    }

    protected virtual void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.CompareTag("Traps"))
        {
            isHit = true;
            healthSystem.Damage(1);
        }
    }

    // the function returns the nearest object from the array, relative to the specified position
    GameObject NearTarget(Vector3 position, Collider2D[] array)
    {
        Collider2D current = null;
        float dist = Mathf.Infinity;

        foreach (Collider2D coll in array)
        {
            float curDist = Vector3.Distance(position, coll.transform.position);

            if (curDist < dist)
            {
                current = coll;
                dist = curDist;
            }
        }

        return current?.gameObject;
    }

    // point - точка контакта
    // radius - радиус поражения
    // layerMask - номер слоя, с которым будет взаимодействие
    // damage - наносимый урон
    // allTargets - должны-ли получить урон все цели, попавшие в зону поражения
    public void Action(Vector2 point, float radius, int layerMask, bool allTargets)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(point, radius, 1 << layerMask);

        if (!allTargets)
        {
            GameObject obj = NearTarget(point, colliders);
            if (obj != null && obj.GetComponent<Creatures>())
            {
                //Identifying the character to be damaged
                if (obj.GetComponent<PlayerMovement>())
                {
                    obj.GetComponent<PlayerMovement>().healthSystem.Damage(10);
                    obj.GetComponent<PlayerMovement>().isHit = true;
                }
                else
                {
                    obj.GetComponent<Creatures>().healthSystem.Damage(33);
                    obj.GetComponent<Creatures>().isHit = true;
                }
            }
            return;
        }
        //Damage to all characters within the radius of attack
        foreach (Collider2D hit in colliders)
        {
            if (hit.GetComponent<Creatures>())
            {
                if (hit.GetComponent<PlayerMovement>())
                {
                    hit.GetComponent<PlayerMovement>().healthSystem.Damage(10);
                    hit.GetComponent<PlayerMovement>().isHit = true;
                }
                else
                {
                    hit.GetComponent<Creatures>().healthSystem.Damage(33);
                    hit.GetComponent<Creatures>().isHit = true;
                }
            }
        }
    }

    protected RaycastHit2D Raycast(Vector2 offset, Vector2 rayDirection, float length)
    {
        //Call the overloaded Raycast() method using the ground layermask and return 
        //the results
        return Raycast(offset, rayDirection, length, groundMask);
    }

    RaycastHit2D Raycast(Vector2 offset, Vector2 rayDirection, float length, LayerMask mask)
    {
        //Record the player's position
        Vector2 pos = transform.position;

        //Send out the desired raycasr and record the result
        RaycastHit2D hit = Physics2D.Raycast(pos + offset, rayDirection, length, mask);

        //If we want to show debug raycasts in the scene...
        if (drawDebugRaycasts)
        {
            //...determine the color based on if the raycast hit...
            Color color = hit ? Color.red : Color.green;
            //...and draw the ray in the scene view
            Debug.DrawRay(pos + offset, rayDirection * length, color);
        }

        //Return the results of the raycast
        return hit;
    }

    protected void Die(bool isDead)
    {
        //If dead, then destroying the object, reset states and adding a dead body
        if (!isDead)
            return;

        isHit = false;
        Vector3 position = transform.position;
        Vector3 scale = transform.localScale;
        rigidBody.velocity = new Vector2(0, rigidBody.velocity.y);
        deadBody.transform.position = position;
        deadBody.transform.localScale = scale;
        if (deadBody == null)
            Destroy(gameObject);
        if (dieTime < Time.time)
        {
            Destroy(gameObject);
            Instantiate(deadBody);
        }
    }
}

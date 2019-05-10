using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    Creatures creature;
    public List<Collider2D> zoneOfVisionOfCreatures;
    public GameObject Parent { get; set; }

    private float speed = 8f;
    public Vector3 Direction { get; set; }

    private RaycastHit2D hit;

    private SpriteRenderer sprite;

    private void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        creature = GetComponentInParent<Creatures>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 1.5f);
    }

    // Update is called once per frame
    void Update()
    {
        //hit = Physics2D.Raycast(transform.position + transform.up * 0.5f + transform.right * direction.x * 0.5f, direction);
        //if (hit.transform.tag != tag)
        //{
        //    Debug.Log(hit.transform.name);
        //    Destroy(gameObject);
        //}

        transform.position = Vector3.MoveTowards(transform.position, transform.position + Direction, speed * Time.deltaTime);
        sprite.flipX = Direction.x < 0.1f;
    }
    private void OnTriggerEnter2D(Collider2D collider)
    {
        Creatures creature = collider.GetComponent<Creatures>();
        if ((creature && creature.gameObject != Parent))
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collider)
    {
        Destroy(gameObject);
    }
}

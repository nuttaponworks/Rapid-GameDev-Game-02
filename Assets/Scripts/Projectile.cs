using UnityEngine;

public class Projectile : MonoBehaviour
{

    public float speed = 10f;
    public float baseDamage = 10f;
    private Vector2 direction;
    private ElementType element;
    
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    public void SetElement(ElementType e)
    {
        element = e;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
       

        if (other.CompareTag("Boss"))
        {
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(baseDamage);
                Debug.Log($"Projectile hit Boss with {element}, damage: {baseDamage}");
            }
            Destroy(gameObject);
        }
    }
}

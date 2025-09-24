using UnityEngine;

public class Projectile : MonoBehaviour
{

    public float speed = 10f;
    public float baseDamage = 10f;
    private Vector2 direction;
    private ElementType element;
    public void SetDirection(Vector2 dir) => direction = dir.normalized;
    [SerializeField] Vector2 bossPo;
    public void SetElement(ElementType e)
    {
        element = e;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            switch (element)
            {
                case ElementType.Fire: sr.color = Color.red; break;
                case ElementType.Water: sr.color = Color.blue; break;
                case ElementType.Wind: sr.color = Color.white; break;
                default: sr.color = Color.gray; break;
            }
        }
    }

    void Update()
    {
        transform.Translate(bossPo * speed * Time.deltaTime);
    }


    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Boss"))
        {
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(baseDamage);
            }
            
            Destroy(gameObject);
        }
    }
}

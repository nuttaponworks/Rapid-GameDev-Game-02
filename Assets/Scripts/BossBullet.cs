using UnityEngine;

public class BossBullet : MonoBehaviour
{
    public float speed = 5f;   
    public float damage = 10f; 
    private Vector2 direction;

    [SerializeField] private GameObject hitParticle;
    public void SetTarget(Vector3 targetPos)
    {
        direction = (targetPos - transform.position).normalized;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        if (Vector3.Distance(Vector3.zero, transform.position) > 50f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStat player = other.GetComponent<PlayerStat>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Instantiate(hitParticle, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }

        else if (other.GetComponent<HomingProjectile>())
        {
            Instantiate(hitParticle, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }
    }
}

using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Transform firePoint;
    public GameObject projectilePrefab;

    public void ShootImmediate(ElementType element)
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile p = proj.GetComponent<Projectile>();

        if (p != null)
        {
            Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            p.SetDirection(dir);

            p.SetElement(element);
        }
    }
}

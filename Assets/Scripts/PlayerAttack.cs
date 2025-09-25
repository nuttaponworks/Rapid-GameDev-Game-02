using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Transform firePoint;
    public GameObject projectilePrefab;

    public void ShootImmediate(ElementType element)
    {
        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss == null) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile p = proj.GetComponent<Projectile>();

        if (p != null)
        {
            Vector2 dir = (boss.transform.position - firePoint.position).normalized;
            p.SetDirection(dir);
            p.SetElement(element);
        }
    }
}

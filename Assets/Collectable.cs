using UnityEngine;

public class Collectable : MonoBehaviour
{
    private ElementType elementType;

    void Start()
    {
        elementType = (ElementType)Random.Range(1, 5);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            switch (elementType)
            {
                case ElementType.Fire: sr.color = Color.red; break;
                case ElementType.Water: sr.color = Color.blue; break;
                case ElementType.Wind: sr.color = Color.white; break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInventory inv = other.GetComponent<PlayerInventory>();
            PlayerAttack atk = other.GetComponent<PlayerAttack>();

            if (inv != null && atk != null)
            {
                inv.SetElement(elementType);

                atk.ShootImmediate(elementType);
            }

            Destroy(gameObject);
        }
    }
}

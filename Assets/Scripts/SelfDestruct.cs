using System.Collections;
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    [SerializeField] private bool destroyOnStart = true;

    [SerializeField] private float destroyDelay = 1f;

    [SerializeField] private GameObject hitParticle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (destroyOnStart) StartCoroutine(DestroyDelayJa());
    }

    IEnumerator DestroyDelayJa()
    {
        yield return new WaitForSeconds(destroyDelay);
        if (hitParticle) Instantiate(hitParticle, this.transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}

using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    [SerializeField] private bool destroyOnStart = true;

    [SerializeField] private float destroyDelay = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(destroyOnStart) Destroy(gameObject,destroyDelay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

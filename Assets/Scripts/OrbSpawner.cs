using UnityEngine;

public class OrbSpawner : MonoBehaviour
{
    public GameObject orbPrefab;
    public float spawnInterval = 3f;
    public Vector2 spawnAreaMin;
    public Vector2 spawnAreaMax;
    void Start()
    {
        InvokeRepeating(nameof(SpawnOrb), 1f, spawnInterval);
    }

    void SpawnOrb()
    {
        Vector2 pos = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        Instantiate(orbPrefab, pos, Quaternion.identity);
    }
}

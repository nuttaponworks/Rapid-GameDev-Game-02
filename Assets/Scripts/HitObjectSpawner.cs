using System;
using TarodevController;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class HitObjectSpawner : MonoBehaviour
{
    [SerializeField] private DashOnlyHitTarget[] hitObject;
    [SerializeField] private DashOnlyHitTarget currentHitObject;
    [SerializeField] private GameObject spawnParticle;

    [Space]
    [SerializeField] private float yOffset = 1;
    [SerializeField] private float minSpawnTime = 5f, maxSpawnTime = 30f, currentSpawnTime;

    private void Start()
    {
        currentSpawnTime = Random.Range(minSpawnTime, maxSpawnTime*5);
    }

    private void Update()
    {
        if (GameStateManager.Instance.currentState != GameState.Process) return;
        if (currentSpawnTime > 0) currentSpawnTime -= Time.deltaTime;

        if (currentHitObject == null && currentSpawnTime <= 0)
        {
            currentSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);

            currentHitObject = Instantiate(hitObject[Random.Range(0,hitObject.Length)], new Vector2(transform.position.x, transform.position.y + yOffset),
                quaternion.identity);
            Instantiate(spawnParticle, new Vector2(transform.position.x, transform.position.y + yOffset), Quaternion.identity);

        }
    }
}

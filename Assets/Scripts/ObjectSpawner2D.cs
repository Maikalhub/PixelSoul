using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner2D : MonoBehaviour
{
    [System.Serializable]
    public class SpawnItem
    {
        [Header("Объект")]
        public GameObject prefab;

        [Header("Настройки этого объекта")]
        public float moveSpeed = 3f;
        public float lifeTime = 10f;

        [Header("Частота появления")]
        public float spawnDelay = 1f;

        [Range(0f, 100f)]
        public float spawnChance = 100f;

        [HideInInspector] public float timer;
    }

    private class SpawnedObject
    {
        public GameObject obj;
        public Transform transform;
        public Rigidbody2D rb;
        public float moveSpeed;
        public float destroyTime;
    }

    [Header("Массив объектов с настройками")]
    [SerializeField] private SpawnItem[] spawnItems;

    private Collider2D spawnCollider;

    private readonly List<SpawnedObject> spawnedObjects = new List<SpawnedObject>();

    private void Awake()
    {
        spawnCollider = GetComponent<Collider2D>();

        if (spawnCollider == null)
        {
            Debug.LogError("На объекте со спавнером должен быть Collider2D!");
        }
    }

    private void Update()
    {
        SpawnTimers();
        CheckLifeTime();
    }

    private void FixedUpdate()
    {
        MoveObjectsLeft();
    }

    private void SpawnTimers()
    {
        if (spawnItems == null || spawnItems.Length == 0 || spawnCollider == null)
            return;

        for (int i = 0; i < spawnItems.Length; i++)
        {
            SpawnItem item = spawnItems[i];

            if (item == null || item.prefab == null)
                continue;

            item.timer += Time.deltaTime;

            if (item.timer >= item.spawnDelay)
            {
                item.timer = 0f;

                float randomChance = Random.Range(0f, 100f);

                if (randomChance <= item.spawnChance)
                {
                    SpawnObject(item);
                }
            }
        }
    }

    private void SpawnObject(SpawnItem selectedItem)
    {
        Vector2 spawnPosition = GetRandomPointInCollider();

        GameObject newObject = Instantiate(
            selectedItem.prefab,
            spawnPosition,
            Quaternion.identity
        );

        SpawnedObject spawned = new SpawnedObject();
        spawned.obj = newObject;
        spawned.transform = newObject.transform;
        spawned.rb = newObject.GetComponent<Rigidbody2D>();
        spawned.moveSpeed = selectedItem.moveSpeed;
        spawned.destroyTime = Time.time + selectedItem.lifeTime;

        spawnedObjects.Add(spawned);
    }

    private void MoveObjectsLeft()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i].obj == null)
            {
                spawnedObjects.RemoveAt(i);
                continue;
            }

            Vector3 currentPosition = spawnedObjects[i].transform.position;

            Vector3 newPosition = new Vector3(
                currentPosition.x - spawnedObjects[i].moveSpeed * Time.fixedDeltaTime,
                currentPosition.y,
                currentPosition.z
            );

            if (spawnedObjects[i].rb != null)
            {
                spawnedObjects[i].rb.MovePosition(newPosition);
            }
            else
            {
                spawnedObjects[i].transform.position = newPosition;
            }
        }
    }

    private void CheckLifeTime()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i].obj == null)
            {
                spawnedObjects.RemoveAt(i);
                continue;
            }

            if (Time.time >= spawnedObjects[i].destroyTime)
            {
                Destroy(spawnedObjects[i].obj);
                spawnedObjects.RemoveAt(i);
            }
        }
    }

    private Vector2 GetRandomPointInCollider()
    {
        Bounds bounds = spawnCollider.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        return new Vector2(x, y);
    }
}
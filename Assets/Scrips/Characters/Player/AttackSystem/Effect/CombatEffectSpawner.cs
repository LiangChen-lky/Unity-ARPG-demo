using UnityEngine;

public interface ICombatEffectSpawner
{
    GameObject SpawnOneShot(GameObject prefab, Vector3 position, Vector3 rotation, Vector3 scale);
}

public sealed class CombatEffectSpawner : ICombatEffectSpawner
{
    public GameObject SpawnOneShot(GameObject prefab, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, position, Quaternion.Euler(rotation));
        instance.transform.localScale = scale;
        return instance;
    }
}

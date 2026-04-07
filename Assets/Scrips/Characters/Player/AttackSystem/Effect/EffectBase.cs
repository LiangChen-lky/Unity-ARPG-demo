using System.Collections;
using UnityEngine;

public class EffectBase : MonoBehaviour
{
    public float Duration { get; private set;}

    protected virtual void Awake()
    {
        StartCoroutine(DestorySelf());
    }

    IEnumerator DestorySelf()
    {
        while (Duration > 0f)
        {
            yield return null;
            Duration -= Time.deltaTime;
        }
        Destroy(gameObject);
    }
}

using UnityEngine;

public class SkinnedMeshRenderUtility : MonoBehaviour
{
    private SkinnedMeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<SkinnedMeshRenderer>();
    }

    private void Start()
    {
        Debug.Log(meshRenderer.bounds.size);
    }
}

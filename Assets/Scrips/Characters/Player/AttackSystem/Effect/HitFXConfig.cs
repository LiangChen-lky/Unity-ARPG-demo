using UnityEngine;

[CreateAssetMenu(fileName = "HitFXConfig", menuName = "ScriptableObject/Combat/HitFXConfig")]
public class HitFXConfig : ScriptableObject
{
    [field: SerializeField] public GameObject[] HitFXList { get; private set; }

    public GameObject TryGetOneFXObject()
    {
        return HitFXList[Random.Range(0, HitFXList.Length)];
    }
    
}

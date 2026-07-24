using UnityEngine;

[CreateAssetMenu(fileName = "HitFXConfig", menuName = "ScriptableObject/Combat/HitFXConfig")]
public class HitFXConfig : ScriptableObject
{
    [field: SerializeField] public GameObject[] HitFXList { get; private set; }

    // 随机返回一个命中特效预制体；配置缺失时返回 null，由调用方决定是否静默。
    public GameObject TryGetOneFXObject()
    {
        if (HitFXList == null || HitFXList.Length == 0)
        {
            return null;
        }

        return HitFXList[Random.Range(0, HitFXList.Length)];
    }
}

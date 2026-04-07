using UnityEngine;

public class ToolManager : Singleton<ToolManager>
{
    // 特效事件, 待优化， 目前是直接实例化特效对象，后续可以考虑使用对象池来管理特效对象的生命周期
    public void PlayOneFX(GameObject FXObject, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        var obj = UnityEngine.Object.Instantiate(FXObject);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(rotation);
        obj.transform.localScale = scale;
    }
}

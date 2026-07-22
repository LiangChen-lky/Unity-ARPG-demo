using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerAttackDetectionGizmo : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private Color attackDetectionColor = Color.yellow;
    [SerializeField, Min(0)] private int previewComboIndex;

    // 只在选中玩家时绘制，避免常驻调试线框遮挡场景内容。
    private void OnDrawGizmosSelected()
    {
        Player player = GetComponent<Player>();
        if (player.Data == null ||
            player.Data.AttackData == null ||
            player.Data.AttackData.CurrentComboList == null)
        {
            return;
        }

        ComboConfig[] comboConfigs = player.Data.AttackData.CurrentComboList.ComboConfigs;
        if (comboConfigs == null)
        {
            return;
        }

        if (previewComboIndex >= comboConfigs.Length)
        {
            return;
        }

        ComboConfig comboConfig = comboConfigs[previewComboIndex];
        if (comboConfig == null || comboConfig.AttackDetectionConfig == null)
        {
            return;
        }

        foreach (AttackDetectionConfig detectionConfig in comboConfig.AttackDetectionConfig)
        {
            if (detectionConfig == null)
            {
                continue;
            }

            DrawDetectionBox(detectionConfig);
        }
    }

    private void DrawDetectionBox(AttackDetectionConfig detectionConfig)
    {
        GetWorldDetectionBox(
            transform,
            detectionConfig,
            out Vector3 center,
            out Quaternion rotation);

        Gizmos.color = attackDetectionColor;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        // OverlapBox 使用半尺寸，DrawWireCube 使用完整尺寸。
        Gizmos.DrawWireCube(Vector3.zero, detectionConfig.Scale * 2f);
        Gizmos.matrix = Matrix4x4.identity;
    }

    // 与攻击检测配置一致地换算局部偏移，供当前 Gizmo 绘制使用。
    private static void GetWorldDetectionBox(
        Transform owner,
        AttackDetectionConfig detectionConfig,
        out Vector3 center,
        out Quaternion rotation)
    {
        Vector3 offset =
            owner.forward * detectionConfig.Position.z +
            owner.up * detectionConfig.Position.y +
            owner.right * detectionConfig.Position.x;

        center = owner.position + offset;
        rotation = Quaternion.Euler(detectionConfig.Rotation + owner.eulerAngles);
    }
#endif
}

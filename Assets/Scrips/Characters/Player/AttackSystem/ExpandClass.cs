using UnityEngine;

public static class ExpandClass
{
    public static Vector3 GetMoveOffsetDirection(this Transform transform, MoveOffsetDirection moveDirection)
    {
        if (moveDirection == MoveOffsetDirection.Up)
        {
            return Vector3.up;
        }
        else if(moveDirection == MoveOffsetDirection.Forward)
        {
            return transform.forward;
        }
        return Vector3.zero;
    }
}

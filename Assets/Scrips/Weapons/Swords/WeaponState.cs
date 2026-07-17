// 武器自身的表现状态，不等同于玩家状态机中的移动或战斗状态。
public enum WeaponState
{
    // 收刀并跟随待机挂点。
    Idle,

    // 出刀并跟随攻击挂点。
    Attacking
}

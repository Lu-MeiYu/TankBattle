using UnityEngine;

namespace TankBattle.Data
{
    /// <summary>
    /// 跨模組協調用的共用設定。只放 Turn Flow、地圖規模、AI 人數上下限等
    /// 需要被多個模組同時讀取的欄位。其餘平衡數值（風力範圍、傷害係數、升級花費曲線、
    /// 地形隨機參數、AI 難度誤差參數）由各自模組擁有獨立的 Config（見 Docs/SharedContracts.md §4），
    /// 不塞進本檔案，以降低平行開發時的檔案衝突面。
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "TankBattle/Data/BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        [Header("Turn Flow")]
        [Min(1f)]
        public float turnTimeLimitSeconds = 30f;

        [Header("Map Scaling")]
        public float mapBaseWidth = 20f;
        public float mapUnitSpacing = 3f;
        public float minSafeSpacing = 2f;

        [Header("Match Rules")]
        [Min(1)]
        public int minAiCount = 3;
        [Min(1)]
        public int maxAiCount = 10;
    }
}

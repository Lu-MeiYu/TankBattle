namespace TankBattle.Core.Shared
{
    /// <summary>
    /// 風力資料。刻意只用單一 <see cref="SignedValue"/>（-10~10）代表風力方向與強度合併值，
    /// 不拆成方向/強度兩個欄位，避免 AI、UI、Ballistics 三邊對「正負代表哪個方向」認知不一致。
    /// 正值代表風向右吹，負值代表風向左吹（對應世界座標 X 軸正方向為右）。
    /// </summary>
    public readonly struct WindData
    {
        public readonly float SignedValue;

        public WindData(float signedValue)
        {
            SignedValue = signedValue;
        }

        public static WindData Zero => new WindData(0f);
    }
}

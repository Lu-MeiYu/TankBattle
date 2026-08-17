namespace TankBattle.Core.Shared
{
    /// <summary>
    /// 風力產生器。US-05：每次發射前重新產生一次風力，同一發射過程中風力保持不變。
    /// <see cref="GenerateNewWind"/> 與 <see cref="CurrentWind"/> 分離，是為了讓「本回合已產生的風」
    /// 在 UI 顯示、AI 決策、實際彈道計算三處都讀到同一份快照，不因呼叫時機不同而各自重算。
    /// 呼叫時機由 Turn/Match Flow（Gameplay 層）在「輪到某坦克發射前」呼叫一次，
    /// Ballistics 本身不主動觸發產生，維持無狀態純函式。
    /// </summary>
    public interface IWindProvider
    {
        WindData CurrentWind { get; }

        WindData GenerateNewWind();
    }
}

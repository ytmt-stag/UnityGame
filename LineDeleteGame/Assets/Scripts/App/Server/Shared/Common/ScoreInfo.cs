namespace App.Shared.Common
{
    /// <summary>
    /// スコア情報
    /// </summary>
    public class ScoreInfo
    {
        const int BASE_SCORE = 250;

        /// <summary>現在のスコア</summary>
        public int Score { get; private set; } = 0;

        /// <summary>
        /// 消した行数でスコア加算
        /// </summary>
        /// <param name="_lineNum"></param>
        public void AddScoreByDeleteLines(int lineNum)
        {
            Score += lineNum * BASE_SCORE;
        }

        /// <summary>
        /// リセット
        /// </summary>
        public void Reset()
        {
            Score = 0;
        }
    }
}
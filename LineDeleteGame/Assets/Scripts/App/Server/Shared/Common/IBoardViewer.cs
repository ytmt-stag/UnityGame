using App.Shared.MessagePackObjects;
using System.Collections.Generic;

namespace App.Shared.Common
{
    /// <summary>
    /// ボード描画インターフェース
    /// </summary>
    public interface IBoardViewer
    {
        /// <summary>
        /// スコア更新
        /// </summary>
        /// <param name="_score"></param>
        void UpdateScore(int score);

        /// <summary>
        /// ボード描画更新
        /// </summary>
        /// <param name="_board"></param>
        void UpdateBoardDrawing(IReadOnlyList<short> board);

        /// <summary>
        /// 次のブロック更新
        /// </summary>
        /// <param name="_first"></param>
        /// <param name="_second"></param>
        void UpdateNextBlockDrawing(BlockStatus first, BlockStatus second);

        /// <summary>
        /// ゲームオーバー描画
        /// </summary>
        /// <param name="_board"></param>
        void DrawGameOver(IReadOnlyList<short> board);

        /// <summary>
        /// 行削除準備
        /// </summary>
        /// <param name="_willDeleteLineY"></param>
        void PrepareLineDeleteEffect(IReadOnlyList<short> willDeleteLineY);

        /// <summary>
        /// 行削除エフェクト更新
        /// </summary>
        /// <param name="_willDeleteLineY"></param>
        /// <param name="_dt"></param>
        /// <param name="_fadeDuration"></param>
        /// <returns></returns>
        bool UpdateLineDeletingEffect(IReadOnlyList<short> willDeleteLineY, float dt, float fadeDuration);
    }

}
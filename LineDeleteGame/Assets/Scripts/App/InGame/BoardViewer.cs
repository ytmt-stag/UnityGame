using App.Shared.Common;
using App.Shared.MessagePackObjects;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace App
{
    /// <summary>
    /// ゲーム盤面 描画と制御
    /// </summary>
    public class BoardViewer : MonoBehaviour, IBoardViewer
    {
        /// <summary>盤面描画</summary>
        [SerializeField]
        private BoardRenderer boardRenderer = null;

        /// <summary>スコア表示</summary>
        [SerializeField]
        private TextMeshPro scoreMesh = null;

        /// <summary>スコアテキスト</summary>
        private System.Text.StringBuilder scoreText = new System.Text.StringBuilder(16);

        /// <summary>変更検知用</summary>
        private int scoreCache = 0;

        /// <summary>変更検知用</summary>
        private List<BlockStatus> blockStatusCache = new List<BlockStatus>(SharedConstant.SHOW_NEXT_BLOCK_NUM);

        /// <summary>
        /// MonoBehaviour
        /// </summary>
        private void Awake()
        {
            blockStatusCache.Add(new BlockStatus());
            blockStatusCache.Add(new BlockStatus());
        }

        /// <summary>
        /// タイル描画更新
        /// </summary>
        /// <param name="board"></param>
        public void UpdateBoardDrawing(IReadOnlyList<short> board)
        {
            boardRenderer.UpdateTilePanel(board);
        }

        /// <summary>
        /// 次のタイル更新
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        public void UpdateNextBlockDrawing(BlockStatus first, BlockStatus second)
        {
            bool isUpdate = false;
            if (first.Type != blockStatusCache[0].Type)
            {
                isUpdate = true;
                blockStatusCache[0] = first;
            }
            if (second.Type != blockStatusCache[1].Type)
            {
                isUpdate = true;
                blockStatusCache[1] = second;
            }

            if (isUpdate)
            {
                boardRenderer.UpdateNextTile(first, second);
            }
        }

        /// <summary>
        /// スコア
        /// </summary>
        /// <param name="score"></param>
        public void UpdateScore(int score)
        {
            if (score == scoreCache)
            {   // 更新必要なければ何もしない
                return;
            }

            scoreCache = score;
            scoreText.Clear();
            scoreText.AppendFormat("{0:D8}", score);
            scoreMesh.text = scoreText.ToString();
        }

        /// <summary>
        /// ゲームオーバー描画
        /// </summary>
        /// <param name="board"></param>
        public void DrawGameOver(IReadOnlyList<short> board)
        {
            boardRenderer.DrawGameOver(board);
        }

        /// <summary>
        /// エフェクト開始
        /// </summary>
        /// <param name="willDeleteLineY"></param>
        public void PrepareLineDeleteEffect(IReadOnlyList<short> willDeleteLineY)
        {
            boardRenderer.Effector.StartEffect(willDeleteLineY);
        }

        /// <summary>
        /// 線消去
        /// </summary>
        /// <param name="willDeleteLineY"></param>
        /// <returns></returns>
        public bool UpdateLineDeletingEffect(IReadOnlyList<short> willDeleteLineY, float dt, float fadeDuration)
        {
            float ratio = boardRenderer.Effector.UpdateEffect(willDeleteLineY, dt, fadeDuration);
            return ratio >= 1.0f;
        }
    }

}
using App.Shared.Common;
using System.Collections.Generic;

namespace App
{
    /// <summary>
    /// 1Pゲーム実行中
    /// </summary>
    public class AlonePlayingState : IMainGameLogicState
    {
        /// <summary>
        /// 1P用情報
        /// </summary>
        public struct AloneInfo
        {
            /// <summary>盤面描画</summary>
            public IBoardViewer viewer;
        }

        /// <summary>盤面描画</summary>
        private IBoardViewer viewer = null;

        /// <summary>落ち終わりState(頻繁なnewを回避)</summary>
        AloneDeleteLineState deleteLineState = null;

        /// <summary>一時停止(過剰なnewを回避)</summary>
        AlonePauseState pauseState = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="render"></param>
        /// <param name="info"></param>
        public AlonePlayingState(LogicInfo info, AloneInfo ctrlInfo) : base(info)
        {
            viewer = ctrlInfo.viewer;

            pauseState = new AlonePauseState(input);
            deleteLineState = new AloneDeleteLineState();
            deleteLineState.InitializeBeforeEnter(boardInfo, viewer);
        }

        /// <summary>
        /// リセット
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            // 次のブロック表示
            viewer.UpdateNextBlockDrawing(nextBlocks[0], nextBlocks[1]);
        }

        /// <summary>
        /// 終了 / 大抵ゲームオーバー
        /// </summary>
        /// <returns></returns>
        public override IState Exit()
        {
            viewer.DrawGameOver(boardInfo.Board);
            return new AloneGameOverState();
        }

        /// <summary>
        /// 描画
        /// </summary>
        /// <param name="dt"></param>
        public override void Draw(float dt)
        {
            viewer.UpdateBoardDrawing(boardInfo.Board);
        }

        /// <summary>
        /// Block情報初期化時
        /// </summary>
        protected override void onBlockDropStart()
        {
            viewer.UpdateNextBlockDrawing(nextBlocks[0], nextBlocks[1]);
        }

        /// <summary>
        /// ポーズ
        /// </summary>
        protected override void onPause()
        {
            Ctrl.ReserveAddState(pauseState);
        }

        /// <summary>
        /// 落ち終わり
        /// </summary>
        protected override void onDeleteLine()
        {
            // スコア更新
            viewer.UpdateScore(scoreInfo.Score);
            // State変更
            Ctrl.ReserveAddState(deleteLineState);
        }
    }

    /// <summary>
    /// ゲーム開始
    /// </summary>
    public class AlonePlayStartState : IState
    {
        /// <summary>次のState渡しておく</summary>
        private IState playing = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="playing"></param>
        public AlonePlayStartState(IState playing)
        {   // Exitで渡すため
            this.playing = playing;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public override bool Update(float dt)
        {   // 開始演出
            return false;
        }

        /// <summary>
        /// 終了
        /// </summary>
        /// <returns></returns>
        public override IState Exit()
        {
            return playing;
        }
    }

    /// <summary>
    /// 一時停止
    /// </summary>
    public class AlonePauseState : IState
    {
        /// <summary>キー入力取得</summary>
        private IPlayInputReader input = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input"></param>
        public AlonePauseState(IPlayInputReader input)
        {
            this.input = input;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public override bool Update(float dt)
        {
            ePlayCommand com = input.GetCurrentCommand();
            if (com == ePlayCommand.PauseAndResume)
            {
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Lineが消えるとき / Network側でも使用
    /// </summary>
    public class AloneDeleteLineState : IState
    {
        /// <summary>ボード情報</summary>
        private BoardInfo boardInfo = null;

        /// <summary>描画情報</summary>
        private IBoardViewer viewer = null;

        /// <summary>落ち切った時ちょっと待機</summary>
        private float duration = 0f;

        /// <summary>消えるであろう行数(new回避のために予め確保)</summary>
        private List<short> willDeleteLine = new List<short>(SharedConstant.MAX_DELETE_LINE_NUM);

        /// <summary>
        /// Enter前の初期化
        /// </summary>
        /// <param name="render"></param>
        /// <param name="info"></param>
        public void InitializeBeforeEnter(BoardInfo info, IBoardViewer viewer)
        {
            this.viewer = viewer;

            boardInfo = info;
            willDeleteLine.Clear();
        }

        /// <summary>
        /// 準備
        /// </summary>
        public override void Enter()
        {
            // 消す演出を作るための布石
            BoardInfo.SearchWillDeleteLineNumber(willDeleteLine, boardInfo.Board);

            // 経過時間初期化
            duration = 0f;

            viewer.PrepareLineDeleteEffect(willDeleteLine);
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public override bool Update(float dt)
        {
            // 比率を計算してα設定
            duration += dt;
            viewer.UpdateLineDeletingEffect(willDeleteLine, dt, SharedConstant.FADE_DURATION);
            return duration <= SharedConstant.WAIT_DELETE_LINE_DURATION;
        }

        /// <summary>
        /// 終了
        /// </summary>
        /// <returns></returns>
        public override IState Exit()
        {
            // 実データも削除
            boardInfo.DeleteLine();
            return base.Exit();
        }
    }

    /// <summary>
    /// ゲームオーバー
    /// </summary>
    public class AloneGameOverState : IState
    {
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public override bool Update(float dt)
        {
            return true;
        }
    }
}
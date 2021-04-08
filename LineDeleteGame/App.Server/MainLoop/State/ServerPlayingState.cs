using App.Server.Hubs;
using App.Shared.Common;
using App.Shared.MessagePackObjects;
using System.Collections.Generic;

namespace App.Server.Looper
{
    /// <summary>
    /// serverでのアップデート
    /// </summary>
    public class ServerPlayingState : IMainGameLogicState
    {
        /// <summary>盤面情報</summary>
        public IReadOnlyList<short> Board { get { return boardInfo.Board; } }

        /// <summary>次のブロック</summary>
        public BlockStatus NextFirst { get { return nextBlocks[0]; } }

        /// <summary>次の次のブロック</summary>
        public BlockStatus NextSecond { get { return nextBlocks[1]; } }

        /// <summary>現在のスコア</summary>
        public int CurrentScore { get { return scoreInfo.Score; } }

        public bool IsDeleteLine { get; private set; } = false;

        /// <summary>Server -> Client 通信インターフェース</summary>
        private MainGameHubServerImpl hubImpl = null;

        /// <summary>落ち終わりState(頻繁なnewを回避)</summary>
        ServerDeleteLineState deleteLineState = null;

        /// <summary>一時停止 / 疎通切断(過剰なnewを回避)</summary>
        ServerPauseState pauseState = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="render"></param>
        /// <param name="info"></param>
        public ServerPlayingState(LogicInfo info, MainGameHubServerImpl hub) : base(info)
        {
            hubImpl = hub;

            pauseState = new ServerPauseState(input);
            deleteLineState = new ServerDeleteLineState();
            deleteLineState.InitializeBeforeEnter(boardInfo);
        }

        /// <summary>
        /// リセット
        /// </summary>
        public override void Reset()
        {
            base.Reset();
        }

        /// <summary>
        /// 終了 / 大抵ゲームオーバー
        /// </summary>
        /// <returns></returns>
        public override IState Exit()
        {
            return null;
        }

        /// <summary>
        /// Block情報初期化時
        /// </summary>
        protected override void onBlockDropStart()
        {
            IsDeleteLine = false;
        }

        /// <summary>
        /// ポーズ
        /// </summary>
        protected override void onPause()
        {
        }

        /// <summary>
        /// 落ち終わり
        /// </summary>
        protected override void onDeleteLine()
        {
            // 行が消えるのでクライアント側で演出再生
            IsDeleteLine = true;

            // State変更
            Ctrl.ReserveAddState(deleteLineState);
        }
    }

    /// <summary>
    /// ゲーム開始
    /// </summary>
    public class ServerPlayStartState : IState
    {
        /// <summary>Server -> Client 通信インターフェース</summary>
        private MainGameHubServerImpl hubImpl = null;

        /// <summary>次のState渡しておく</summary>
        private IState playing = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="playing"></param>
        public ServerPlayStartState(IState playing, MainGameHubServerImpl hub)
        {   // Exitで渡すため
            this.playing = playing;
            hubImpl = hub;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public override bool Update(float dt)
        {   // クライアントの準備が整うまで待つ
            return !hubImpl.IsStarted;
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
    public class ServerPauseState : IState
    {

        /// <summary>キー入力取得</summary>
        private IPlayInputReader input = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input"></param>
        public ServerPauseState(IPlayInputReader input)
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
    /// ブロック落ち切ったState
    /// </summary>
    public class ServerDeleteLineState : IState
    {
        /// <summary>ボード情報</summary>
        private BoardInfo boardInfo = null;

        /// <summary>落ち切った時ちょっと待機</summary>
        private float duration = 0f;

        /// <summary>
        /// Enter前の初期化
        /// </summary>
        /// <param name="render"></param>
        /// <param name="info"></param>
        public void InitializeBeforeEnter(BoardInfo info)
        {
            boardInfo = info;
        }

        /// <summary>
        /// 準備
        /// </summary>
        public override void Enter()
        {
            // 経過時間初期化
            duration = 0f;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public override bool Update(float dt)
        {
            duration += dt;
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
    public class ServerGameOverState : IState
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


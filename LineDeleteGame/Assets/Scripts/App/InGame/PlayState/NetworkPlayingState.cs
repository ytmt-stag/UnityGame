using App.Server.Hubs;
using App.Shared.Common;
using App.Shared.MessagePackObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// ネットワークゲーム実行中
    /// </summary>
    public class NetworkPlayingState : IState
    {
        /// <summary>自分か否か</summary>
        public bool IsMine { get; private set; } = false;

        /// <summary>サーバーからの情報</summary>
        private ReceivedGameProcessBoardInfo processInfo = null;

        /// <summary>描画用</summary>
        public IBoardViewer Viewer { get; private set; } = null;

        /// <summary>行消し中</summary>
        private NetworkDeleteLineState deleteLineState = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="render"></param>
        /// <param name="info"></param>
        public NetworkPlayingState(IBoardViewer viewer, bool isMine)
        {
            Viewer = viewer;
            IsMine = isMine;

            deleteLineState = new NetworkDeleteLineState();
        }

        /// <summary>
        /// サーバーからの情報受け取り
        /// </summary>
        /// <param name="info"></param>
        public void UpdateProcessInfo(ReceivedGameProcessBoardInfo info)
        {
            processInfo = info;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public override bool Update(float dt)
        {
            if (processInfo == null)
            {
                return true;
            }

            // 盤面情報更新
            Viewer.UpdateBoardDrawing(processInfo.Board);
            Viewer.UpdateScore(processInfo.Score);
            Viewer.UpdateNextBlockDrawing(processInfo.BlockStatuses[0], processInfo.BlockStatuses[1]);

            // 行数消しエフェクト再生へ
            if (processInfo.IsDeleteLine)
            {
                deleteLineState.InitializeBeforeEnter(processInfo.Board, Viewer);
                Ctrl.ReserveAddState(deleteLineState);

                processInfo = null;
            }

            return true;
        }

        /// <summary>
        /// 終了 / 大抵ゲームオーバー
        /// </summary>
        /// <returns></returns>
        public override IState Exit()
        {
            return new AloneGameOverState();
        }
    }

    /// <summary>
    /// ゲーム開始
    /// </summary>
    public class NetworkPlayStartState : IState
    {
        /// <summary>Client -> Server</summary>
        private IMainGameHub toServerImpl = null;

        /// <summary>次のState渡しておく</summary>
        private NetworkPlayingState playing = null;

        /// <summary>ゲーム開始のリクエスト</summary>
        GameStartRequest startRequest = null;

        /// <summary>落ち切った時ちょっと待機</summary>
        private float duration = 0f;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="playing"></param>
        public NetworkPlayStartState(NetworkPlayingState playing, IMainGameHub impl, GameStartRequest reqeust)
        {   // Exitで渡すため
            this.playing = playing;

            toServerImpl = impl;
            startRequest = reqeust;
        }

        /// <summary>
        /// 開始
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
            duration += duration >= 0 ? dt : 0;
            if (0 <= duration && duration <= 2f)
            {   // 開始演出
                return true;
            }
            else if (duration >= 2f)
            {   // durationに無効な値を入れてこれ以上発動しないように
                duration = -1;
                if (playing.IsMine)
                {
                    Debug.Log("Fire Start");
                    _ = connectFire();
                }
            }

            // 発動したらPlayingにそのまま行ってよい
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

        /// <summary>
        /// 接続開始
        /// </summary>
        /// <returns></returns>
        private async Task connectFire()
        {
            await toServerImpl.GameStartAsync(startRequest);
        }
    }

    /// <summary>
    /// 一時停止
    /// </summary>
    public class NetworkPauseState : IState
    {
        /// <summary>キー入力取得</summary>
        private IPlayInputReader input = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input"></param>
        public NetworkPauseState(IPlayInputReader input)
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
    /// Lineが消えるとき
    /// </summary>
    public class NetworkDeleteLineState : IState
    {
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
        public void InitializeBeforeEnter(IReadOnlyList<short> board, IBoardViewer viewer)
        {
            this.viewer = viewer;

            // 消す演出を作るための布石
            BoardInfo.SearchWillDeleteLineNumber(willDeleteLine, board);
        }

        /// <summary>
        /// 準備
        /// </summary>
        public override void Enter()
        {
            // 経過時間初期化
            duration = 0f;

            // エフェクト再生開始
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
            return base.Exit();
        }
    }

    /// <summary>
    /// ゲームオーバー
    /// </summary>
    public class NetworkGameOverState : IState
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
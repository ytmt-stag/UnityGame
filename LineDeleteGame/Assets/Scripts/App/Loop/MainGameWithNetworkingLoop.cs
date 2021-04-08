using App.Server.Hubs;
using App.Shared.Common;
using App.Shared.MessagePackObjects;
using AppLib;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace App
{
    /// <summary>
    /// ネットワークプレイ
    /// </summary>
    public class MainGameWithNetworkingLoop : ILoop, IMainGameHubReceiver
    {
        /// <summary>
        /// 複数実行用
        /// </summary>
        public class PlayerInfo
        {
            /// <summary>現在のPlaying状況</summary>
            public NetworkPlayingState PlayState { get; private set; }

            /// <summary>描画用</summary>
            public BoardViewer Viewer { get; } = null;

            /// <summary>StateController</summary>
            public StateController Ctrl { get; } = new StateController();

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="viewer"></param>
            public PlayerInfo(BoardViewer viewer)
            {
                Viewer = viewer;
            }

            /// <summary>
            /// PlayingState設定
            /// </summary>
            /// <param name="state"></param>
            public void SetPlayState(NetworkPlayingState state)
            {
                PlayState = state;
            }
        }

        /// <summary>自分</summary>
        public const int MINE_IDX = SharedConstant.PLAYER_MINE_IDX;

        /// <summary>相手</summary>
        public const int ENEMY_IDX = SharedConstant.PLAYER_ENEMY_IDX;

        /// <summary>Client -> Server</summary>
        public IMainGameHub ServerImpl { get { return hub.ServerImpl; } }

        /// <summary>Loopパラメータ</summary>
        private ParamMainGameWithNetwork loopParam = null;

        /// <summary>comment</summary>
        private List<PlayerInfo> playerInfo = new List<PlayerInfo>();

        /// <summary>Streaming接続</summary>
        private HubConnector<IMainGameHub, IMainGameHubReceiver> hub = null;

        /// <summary>マッチング参加中</summary>
        private bool isJoin = false;

        /// <summary>キー入力</summary>
        private KeyPlayInput input = null;

        /// <summary>
        /// 開始時初期化
        /// </summary>
        /// <returns></returns>
        async public override UniTask Enter(IBridgeLoopParam param)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

            loopParam = param as ParamMainGameWithNetwork;
            input = new KeyPlayInput();

            // ボードのロード

            // 自分と、対戦の場合は対戦相手
            playerInfo.Add(new PlayerInfo(loadBoardViewer(true, loopParam.WithOther)));
            if (loopParam.WithOther)
            {
                playerInfo.Add(new PlayerInfo(loadBoardViewer(false, loopParam.WithOther)));
            }

            // 接続開始
            connectStart().Forget();
        }

        /// <summary>
        /// 接続開始
        /// </summary>
        /// <returns></returns>
        private async UniTask connectStart()
        {
            while (loopExecuter.IsFading)
            {   // フェード待ち
                await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
            }

            // 接続待ち
            try
            {
                hub = new HubConnector<IMainGameHub, IMainGameHubReceiver>(this, SharedConstant.GRPC_CONNECT_ADDRESS, SharedConstant.GRPC_CONNECT_PORT);
                await hub.ConnectStartAsync();

                // 接続開始
                toGameReadyAsync();
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
            }
        }

        /// <summary>
        /// 終了時
        /// </summary>
        /// <returns></returns>
        public async override UniTask Exit(eLoop nextLoop)
        {
            // 切断待機
            Debug.Log("ExitLoop");
            await disposeConnect();
        }

        /// <summary>
        /// 戻る操作押したとき / 既存処理を上書きしてタイトルまで戻す
        /// </summary>
        /// <returns></returns>
        public override bool OnBack()
        {
            loopExecuter.ReturnToTitle();
            return true;
        }

        /// <summary>
        /// 破棄
        /// </summary>
        async void OnDestroy()
        {
            await disposeConnect();
        }

        /// <summary>
        /// 切断
        /// </summary>
        /// <returns></returns>
        private async UniTask disposeConnect()
        {
            Debug.Log("MainGameWithNetworking Destroy Start");
            await hub.DisposeConnectAsync();
            Debug.Log("MainGameWithNetworking Destroy Complete");
        }

        /// <summary>
        /// ゲーム準備終わり
        /// </summary>
        /// <param name="response"></param>
        public void OnGameReady(GameReadyResponse response)
        {
            Debug.Log($"GameReady!! at {loopParam.MineId}");
            prepareGameStart(response);
        }

        /// <summary>
        /// ゲーム開始
        /// </summary>
        /// <param name="response"></param>
        public void OnGameStart(GameStartResponse response)
        {
            Debug.Log($"GameStart!! at {loopParam.MineId}");
        }

        /// <summary>
        /// ゲームの状況取得
        /// </summary>
        /// <param name="response"></param>
        public void OnGameProcess(GameProcessResponse response)
        {
            var processInfo = response.PlayersInfo;
            foreach (var key in processInfo.Keys)
            {
                bool isMine = key == loopParam.MineId;
                var info = isMine ? playerInfo[MINE_IDX] : playerInfo[ENEMY_IDX];
                var eachProcess = processInfo[key];

                info.PlayState.UpdateProcessInfo(eachProcess);
            }
        }

        /// <summary>
        /// MonoBehaviour Update
        /// </summary>
        private void Update()
        {
            if (isJoin)
            {
                input.UpdateFrameProcess(Time.deltaTime);
                // State更新
                foreach (var st in playerInfo)
                {
                    st.Ctrl.Run(Time.deltaTime);
                }

                var com = input.GetCurrentCommand();
                switch (com)
                {   // ブロック操作のみ許容
                    case ePlayCommand.Rotate:
                    case ePlayCommand.Right:
                    case ePlayCommand.Left:
                    case ePlayCommand.Down:
                    case ePlayCommand.DownImmediately:
                        {
                            _ = hub.ServerImpl.InputAsync(new GameInputRequest { Id = loopParam.MineId, Command = com });
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// ViewerのLoad
        /// </summary>
        /// <param name="isMine"></param>
        /// <param name="withOther"></param>
        /// <returns></returns>
        private BoardViewer loadBoardViewer(bool isMine, bool withOther)
        {
            // ボードのロード
            GameObject loadedObj = Instantiate(Resources.Load("Prefabs/Board/Board")) as GameObject;
            var viewer = loadedObj != null ? loadedObj.GetComponent<BoardViewer>() : null;
            Assert.IsTrue(viewer != null, "Boardとしての機能がない");
            loadedObj.SetParentToLocalInitialize(gameObject);

            if (withOther)
            {   // 2Pプレイならずらす
                Vector3 pos = isMine ? new Vector3(3.0f, 0.0f, 0.0f) : new Vector3(-4.0f, 0.0f, 0.0f);
                loadedObj.transform.position = pos;
            }

            return viewer;
        }

        /// <summary>
        /// Game Start
        /// </summary>
        private void prepareGameStart(GameReadyResponse response)
        {
            foreach (var key in response.PlayerBoards.Keys)
            {
                bool isMine = loopParam.MineId == key;
                PlayerInfo info = isMine ? playerInfo[MINE_IDX] : playerInfo[ENEMY_IDX];

                var playing = new NetworkPlayingState(info.Viewer, isMine);
                info.SetPlayState(playing);

                NetworkPlayStartState start = new NetworkPlayStartState(playing, ServerImpl, new GameStartRequest { Id = key, RoomName = loopParam.RoomName });
                info.Ctrl.ReserveAddState(start);
            }
        }

        /// <summary>
        /// 接続・切断
        /// </summary>
        private async void toGameReadyAsync()
        {
            if (!isJoin)
            {   // 接続開始
                var request = new GameReadyRequest { MineId = loopParam.MineId, EnemyId = loopParam.EnemyId, RoomName = loopParam.RoomName, WithOther = loopParam.WithOther };
                await hub.ServerImpl.GameReadyAsync(request);

                isJoin = true;
            }
            else
            {   // 切断
                await hub.ServerImpl.LeaveAsync();

                // UI初期化
                isJoin = false;
            }
        }
    }

    /// <summary>
    /// マッチング -> NetworkLoopへの受け渡し
    /// </summary>
    public class ParamMainGameWithNetwork : IBridgeLoopParam
    {
        /// <summary>自分のID</summary>
        public string MineId { get; set; }

        /// <summary>相手のID</summary>
        public string EnemyId { get; set; }

        /// <summary>部屋名</summary>
        public string RoomName { get; set; } = "";

        /// <summary>誰かと一緒か</summary>
        public bool WithOther { get; set; } = true;
    }
}
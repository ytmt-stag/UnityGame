using App.Server.Looper;
using App.Shared.MessagePackObjects;
using Cysharp.Threading;
using MagicOnion.Server.Hubs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Server.Hubs
{
    /// <summary>
    /// ゲームプレイ中
    /// </summary>
    public class MainGameHubServerImpl : StreamingHubBase<IMainGameHub, IMainGameHubReceiver>, IMainGameHub
    {
        /// <summary>接続者のID</summary>
        public string MineId { get; private set; } = "";

        /// <summary>敵のID</summary>
        public string EnemyId { get; private set; } = "";

        /// <summary>2Pか1Pか</summary>
        public bool WithOther { get; private set; } = false;

        /// <summary>接続者のキー入力</summary>
        public NetworkInput NetworkInput { get; private set; } = new NetworkInput();

        /// <summary>接続中か</summary>
        public bool IsConnected { get; private set; } = false;

        /// <summary>開始済みか</summary>
        public bool IsStarted { get; private set; } = false;

        /// <summary>ロガー</summary>
        private readonly ILogger logger;

        /// <summary>ゲームループのスレッドプール</summary>
        private readonly ILogicLooperPool looperPool;

        /// <summary>対戦部屋</summary>
        private IGroup gameRoom = null;

        /// <summary>new回数が多いので予め確保</summary>
        private GameProcessResponse gameProcessResponse = new GameProcessResponse();

        /// <summary>
        /// DIコンストラクタ
        /// </summary>
        /// <param name="mgr"></param>
        /// <param name="logger"></param>
        public MainGameHubServerImpl(ILogger<MatchingHubServerImpl> logger, ILogicLooperPool looperPool)
        {
            this.logger = logger;
            this.looperPool = looperPool;
        }

        /// <summary>
        /// >マッチング時に与えられたルーム名をもとにゲーム準備を宣言
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task GameReadyAsync(GameReadyRequest request)
        {
            IsConnected = true;
            MineId = request.MineId;
            EnemyId = request.EnemyId;
            WithOther = request.WithOther;

            // ゲームループ作成
            ServerMainGameLoop.LoopData data = new ServerMainGameLoop.LoopData(MineId, EnemyId, this, NetworkInput);
            ServerMainGameLoop.CreateNew(looperPool, logger, data);

            // ゲーム部屋に放り込む
            gameRoom = await Group.AddAsync(request.RoomName);
            if (WithOther)
            {   // 2P
                var memberNum = await gameRoom.GetMemberCountAsync();
                if (memberNum == 2)
                {   // 2人揃ったらゲームを開始してよい
                    logger.LogInformation($"GameReady at {MineId}");
                    var param = new Dictionary<string, IReadOnlyList<short>>();
                    // 互いの情報取得
                    var mineLoop = ServerMainGameLoop.TryTakeGame(MineId);
                    var enemLoop = ServerMainGameLoop.TryTakeGame(EnemyId);
                    param[MineId] = mineLoop.PlayingState.Board;
                    param[EnemyId] = enemLoop.PlayingState.Board;

                    var res = new GameReadyResponse { RoomName = request.RoomName, PlayerBoards = param };
                    Broadcast(gameRoom).OnGameReady(res);
                }
            }
            else
            {   // 1P / 速攻でスタート
                logger.LogInformation($"GameReady at {MineId}");
                var loop = ServerMainGameLoop.TryTakeGame(request.MineId);
                var param = new Dictionary<string, IReadOnlyList<short>>();
                param[MineId] = loop.PlayingState.Board;
                BroadcastToSelf(gameRoom).OnGameReady(new GameReadyResponse { RoomName = request.RoomName, PlayerBoards = param });
            }
        }

        /// <summary>
        /// ゲーム開始
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task GameStartAsync(GameStartRequest request)
        {
            IsStarted = true;
            if (WithOther)
            {   // 2P
                logger.LogInformation($"GameStart at {MineId}");
                var param = new Dictionary<string, IReadOnlyList<short>>();
                // 2P / 互いの情報取得
                var mineLoop = ServerMainGameLoop.TryTakeGame(MineId);
                var enemLoop = ServerMainGameLoop.TryTakeGame(EnemyId);
                param[MineId] = mineLoop.PlayingState.Board;
                param[EnemyId] = enemLoop.PlayingState.Board;

                BroadcastToSelf(gameRoom).OnGameStart(new GameStartResponse { RoomName = request.RoomName, PlayerBoards = param });
            }
            else
            {   // 1人の場合は速攻で始める
                logger.LogInformation($"GameStart at {MineId}");
                var loop = ServerMainGameLoop.TryTakeGame(MineId);
                var param = new Dictionary<string, IReadOnlyList<short>>();
                param[MineId] = loop.PlayingState.Board;
                BroadcastToSelf(gameRoom).OnGameStart(new GameStartResponse { RoomName = request.RoomName, PlayerBoards = param });
            }

            await Task.Yield();
        }

        /// <summary>
        /// 入力情報
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task InputAsync(GameInputRequest request)
        {
            NetworkInput.SetCurrentCommand(request.Command);
            await Task.Yield();
        }

        /// <summary>
        /// ゲームアップデート中に来る
        /// </summary>
        /// <param name="board"></param>
        public void GameUpdateProcess(Dictionary<string, ReceivedGameProcessBoardInfo> info)
        {
            if (!IsStarted)
            {
                return;
            }

            // 毎フレ実行されるので予めResponseはnew確保
            gameProcessResponse.PlayersInfo = info;
            BroadcastToSelf(gameRoom).OnGameProcess(gameProcessResponse);
        }

        /// <summary>
        /// 切断時
        /// </summary>
        /// <returns></returns>
        public async Task LeaveAsync()
        {
            // ゲームルームから外れる
            IsConnected = false;
            await gameRoom.RemoveAsync(Context);
        }

        /// <summary>
        /// 接続開始時
        /// </summary>
        /// <returns></returns>
        protected override ValueTask OnConnecting()
        {
            return CompletedTask;
        }

        /// <summary>
        /// 切断時
        /// </summary>
        /// <returns></returns>
        protected override ValueTask OnDisconnected()
        {
            IsConnected = false;
            _ = gameRoom.RemoveAsync(Context);
            return CompletedTask;
        }
    }
}

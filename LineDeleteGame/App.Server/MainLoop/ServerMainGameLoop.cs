using App.Server.Hubs;
using App.Shared.Common;
using App.Shared.MessagePackObjects;
using Cysharp.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace App.Server.Looper
{
    /// <summary>
    /// サーバー側メインループ / プレイヤー1人1人が個別にループを持ってる
    /// </summary>
    class ServerMainGameLoop
    {
        /// <summary>
        /// 初期化用
        /// </summary>
        public class LoopData
        {
            /// <summary>自分のID</summary>
            public string MineId { get; }
            /// <summary>相手のID</summary>
            public string EnemyId { get; }
            /// <summary>Server -> Client</summary>
            public MainGameHubServerImpl HubImpl { get; }
            /// <summary>入力インターフェース</summary>
            public IPlayInputReader Input { get; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="mineId"></param>
            /// <param name="enemyId"></param>
            /// <param name="hub"></param>
            /// <param name="reader"></param>
            public LoopData(string mineId, string enemyId, MainGameHubServerImpl hub, IPlayInputReader reader)
            {
                MineId = mineId;
                EnemyId = enemyId;
                HubImpl = hub;
                Input = reader;
            }
        }

        /// <summary>全員分のゲームループ</summary>
        public static ConcurrentDictionary<string, ServerMainGameLoop> All { get; } = new ConcurrentDictionary<string, ServerMainGameLoop>();

        /// <summary>ループID</summary>
        public int LooperId { get; }

        /// <summary>ロガー</summary>
        private readonly ILogger logger;

        /// <summary>ループシーケンス</summary>
        private static int gameLoopSeq = 0;

        /// <summary>固有のループ情報</summary>
        private LoopData loopData = null;

        /// <summary>固有のプレイ情報</summary>
        public ServerPlayingState PlayingState { get; private set; }

        /// <summary>StateController</summary>
        private StateController stateCtrl = new StateController();

        /// <summary>現在のメンバーボード情報（キャッシュ）</summary>
        private Dictionary<string, ReceivedGameProcessBoardInfo> memberBoardInfo = new Dictionary<string, ReceivedGameProcessBoardInfo>(2);

        /// <summary>相手のボード情報を参照中に書き換えしないようにスレッドロック用</summary>
        private static Object threadLockObj = new object();

        /// <summary>new防止用にcapacity指定の上先行確保</summary>
        private List<short> enemyBoard = new List<short>(SharedConstant.BOARD_ARRAY_SIZE);

        /// <summary>
        /// 新規ゲームを生成し、ゲームループをスレッドプールに登録
        /// </summary>
        /// <param name="looperPool"></param>
        /// <param name="logger"></param>
        public static void CreateNew(ILogicLooperPool looperPool, ILogger logger, LoopData loopData)
        {
            ServerPlayingState.LogicInfo info;
            info.input = loopData.Input;
            info.threadLockObj = threadLockObj;

            // ゲームループを登録
            var gameLoop = new ServerMainGameLoop(logger, loopData, info);
            looperPool.RegisterActionAsync(gameLoop.UpdateFrame);
        }

        /// <summary>
        /// Loop取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ServerMainGameLoop TryTakeGame(string id)
        {
            if (All.TryGetValue(id, out ServerMainGameLoop loop))
            {
                return loop;
            }
            return null;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger"></param>
        private ServerMainGameLoop(ILogger logger, LoopData loopData, ServerPlayingState.LogicInfo info)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            LooperId = Interlocked.Increment(ref gameLoopSeq);
            this.logger.LogInformation($"{nameof(ServerMainGameLoop)}[{LooperId}]: Register");

            // 固有のループ情報
            this.loopData = loopData;

            // Response用の変数を事前確保
            for (int i = 0; i < SharedConstant.BOARD_ARRAY_SIZE; ++i)
            {   // 予め足しとく
                enemyBoard.Add(0);
            }

            // プレイ中Stateはこちらでも把握したいので保持 / Startに渡しつつ、開始はStartStateから実行されるように
            PlayingState = new ServerPlayingState(info, loopData.HubImpl);
            ServerPlayStartState start = new ServerPlayStartState(PlayingState, loopData.HubImpl);
            stateCtrl.ReserveAddState(start);

            // みんなから参照できるように
            All.TryAdd(loopData.MineId, this);
        }

        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public bool UpdateFrame(in LogicLooperActionContext ctx)
        {
            if (ctx.CancellationToken.IsCancellationRequested)
            {   // If LooperPool begins shutting down, IsCancellationRequested will be `true`.
                logger.LogInformation($"{nameof(ServerMainGameLoop)}[{LooperId}]: Shutdown");
                return false;
            }

            if (loopData.HubImpl == null || !loopData.HubImpl.IsConnected)
            {   // 現状再接続は許容しない仕様 / 再入室を許可するためにはRedis等、接続状況に依存しない別の保存領域が必要
                RemoveGame(loopData.MineId);
                return false;
            }

            // State更新
            stateCtrl.Run((float)ctx.ElapsedTimeFromPreviousFrame.TotalSeconds);

            // 自分のボード情報取得
            updateMemberBoardInfo(loopData.MineId, PlayingState.Board, PlayingState.CurrentScore, PlayingState.NextFirst, PlayingState.NextSecond, PlayingState.IsActive);

            // 敵のボー所情報取得
            if (loopData.HubImpl.WithOther)
            {
                var enem = TryTakeGame(loopData.EnemyId);
                if (enem != null)
                {
                    BlockStatus first = new BlockStatus();
                    BlockStatus second = new BlockStatus();
                    int enemScore = 0;
                    bool isActive = true;

                    lock (threadLockObj)
                    {   // 敵の値は別のスレッドが書き込みしてる可能性があるのでlockする
                        for (int i = 0; i < SharedConstant.BOARD_ARRAY_SIZE; ++i)
                        {
                            enemyBoard[i] = enem.PlayingState.Board[i];
                        }
                        enemScore = enem.PlayingState.CurrentScore;
                        first = enem.PlayingState.NextFirst;
                        second = enem.PlayingState.NextSecond;
                        isActive = enem.PlayingState.IsActive;
                    }

                    updateMemberBoardInfo(loopData.EnemyId, enemyBoard, enemScore, first, second, isActive);
                }
            }

            // ボード情報を送信
            loopData.HubImpl.GameUpdateProcess(memberBoardInfo);

            return true;
        }

        /// <summary>
        /// 内部で持ってるボード情報更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="board"></param>
        /// <param name="score"></param>
        private void updateMemberBoardInfo(string id, IReadOnlyList<short> board, int score, BlockStatus nextFirst, BlockStatus nextSecond, bool playingStateActive)
        {
            if (!memberBoardInfo.ContainsKey(id))
            {
                memberBoardInfo[id] = new ReceivedGameProcessBoardInfo();
            }

            var info = memberBoardInfo[id];
            info.Board = board;
            info.Score = score;
            info.IsDeleteLine = BoardInfo.HasDeleteLine(board) && !playingStateActive;

            info.BlockStatuses.Clear();
            info.BlockStatuses.Add(nextFirst);
            info.BlockStatuses.Add(nextSecond);
        }

        /// <summary>
        /// 更新対象から除外
        /// </summary>
        /// <param name="id"></param>
        public void RemoveGame(string id)
        {   // ゲームから除外
            All.TryRemove(id, out ServerMainGameLoop _);
        }
    }
}

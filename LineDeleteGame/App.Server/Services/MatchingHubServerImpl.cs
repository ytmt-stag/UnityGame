using App.Shared.MessagePackObjects;
using MagicOnion.Server.Hubs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace App.Server.Hubs
{
    /// <summary>
    /// [Streaming]マッチングAPI
    /// 1クライアント当たり1インスタンス
    /// </summary>
    public class MatchingHubServerImpl : StreamingHubBase<IMatchingHub, IMatchingHubReceiver>, IMatchingHub
    {
        /// <summary>ログ</summary>
        private readonly ILogger logger;
        /// <summary>マッチング部屋に放り込む</summary>
        private IGroup matchingRoom = null;

        /// <summary>部屋管理用</summary>
        private readonly MatchingRoomManager roomManager;

        /// <summary>
        /// DIコンストラクタ
        /// </summary>
        /// <param name="mgr"></param>
        /// <param name="logger"></param>
        public MatchingHubServerImpl(MatchingRoomManager mgr, ILogger<MatchingHubServerImpl> logger)
        {
            roomManager = mgr;
            this.logger = logger;
        }

        /// <summary>
        /// マッチング参加(マッチングはでき次第通知)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task JoinAsync(MatchingJoinRequest request)
        {
            // 待合室に放り込む
            matchingRoom = await Group.AddAsync("MatchingRoom");

            if (request.withOther)
            {   // 誰かとマッチングして対戦
                // マッチング処理(今は単純にアクセスしてきた順にマッチングさせる)
                MatchingRoomManager.Data data = new MatchingRoomManager.Data(ConnectionId, Context);
                (string, MatchingRoomManager.Data) addResult = roomManager.TryMatching(data);
                if (!string.IsNullOrEmpty(addResult.Item1))
                {
                    // マッチング対象から外す
                    roomManager.RemoveFromMatchingRoom(data.Id);
                    roomManager.RemoveFromMatchingRoom(addResult.Item2.Id);

                    // マッチングした者同士、部屋番号だけ教えるのであとは対戦サーバーにて再集結させる
                    var mineId = ConnectionId.ToString();
                    var enemyId = addResult.Item2.Guid.ToString();
                    BroadcastToSelf(matchingRoom).OnMatchingSuccess(new MatchingRequestSuccessResponse { MineId = mineId, EnemeyId = enemyId, RoomName = addResult.Item1 });
                    BroadcastTo(matchingRoom, addResult.Item2.Guid).OnMatchingSuccess(new MatchingRequestSuccessResponse { MineId = enemyId, EnemeyId = mineId, RoomName = addResult.Item1 });

                    // マッチングルーム本体から退出させる
                    await matchingRoom.RemoveAsync(addResult.Item2.Context);
                    await matchingRoom.RemoveAsync(Context);
                }
            }
            else
            {   // 1人でネットワーク(スコア更新用) / 特にすることもないので専用の部屋を作ってプレイしてもらう
                BroadcastToSelf(matchingRoom).OnMatchingSuccess(new MatchingRequestSuccessResponse { MineId = ConnectionId.ToString(), EnemeyId = "", RoomName = roomManager.GenerateRoomId() });

                // マッチングルーム本体から退出させる
                await matchingRoom.RemoveAsync(Context);
            }
        }

        /// <summary>
        /// 切断時
        /// </summary>
        /// <returns></returns>
        public async Task LeaveAsync()
        {
            // マッチングから外す
            roomManager.RemoveFromMatchingRoom(ConnectionId.ToString());
            await matchingRoom.RemoveAsync(Context);
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
            // 待ち受けのIDからは消す
            // マッチングから外す
            roomManager.RemoveFromMatchingRoom(ConnectionId.ToString());
            _ = matchingRoom.RemoveAsync(Context);
            return CompletedTask;
        }
    }
}
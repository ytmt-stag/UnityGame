using App.Server.Hubs;
using App.Shared.Common;
using App.Shared.MessagePackObjects;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// マッチング
    /// </summary>
    public class MatchingLoop : ILoop, IMatchingHubReceiver
    {
        /// <summary>接続中テキスト</summary>
        [SerializeField]
        private Text connectingText = null;

        /// <summary>Streaming接続</summary>
        private HubConnector<IMatchingHub, IMatchingHubReceiver> hubConnector = null;

        /// <summary>マッチング参加中</summary>
        private bool isJoin = false;

        private string CONNECT_TEXT { get { return isJoin ? "MATCHING" : "CONNECTING"; } }

        /// <summary>Loopパラメータ</summary>
        private ParamMatchingLoop loopParam = null;

        /// <summary>戻る操作でタイトル帰れるように</summary>
        public override bool EnableBack
        {
            get
            {   // マッチングが確定するまでは許容
                return string.IsNullOrEmpty(roomName);
            }
        }

        /// <summary>部屋名</summary>
        private string roomName = "";

        /// <summary>
        /// 開始時初期化
        /// </summary>
        /// <returns></returns>
        public async override UniTask Enter(IBridgeLoopParam param)
        {
            loopParam = param as ParamMatchingLoop;
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

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

            // UI表示開始
            connectingUI().Forget();

            // 接続待ち
            try
            {
                hubConnector = new HubConnector<IMatchingHub, IMatchingHubReceiver>(this, SharedConstant.GRPC_CONNECT_ADDRESS, SharedConstant.GRPC_CONNECT_PORT);
                await hubConnector.ConnectStartAsync();

                // 接続開始
                joinOrLeave();
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
            Debug.Log("LobbyLoop Destroy Start");
            await hubConnector.DisposeConnectAsync();
            Debug.Log("LobbyLoop Destroy Complete");
        }

        /// <summary>
        /// 接続・切断
        /// </summary>
        private async void joinOrLeave()
        {
            if (!isJoin)
            {   // 接続開始
                var request = new MatchingJoinRequest { withOther = loopParam.WithOther };
                await hubConnector.ServerImpl.JoinAsync(request);

                isJoin = true;
            }
            else
            {   // 切断
                await hubConnector.ServerImpl.LeaveAsync();

                // UI初期化
                isJoin = false;
            }
        }

        /// <summary>
        /// 接続中
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid connectingUI()
        {
            int counter = 0;
            connectingText.text = CONNECT_TEXT;
            while (!this.GetCancellationTokenOnDestroy().IsCancellationRequested && string.IsNullOrEmpty(roomName))
            {
                connectingText.text += ".";
                if (counter >= 3)
                {
                    connectingText.text = CONNECT_TEXT;
                    counter = 0;
                }
                else
                {
                    counter++;
                }

                await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }

        /// <summary>
        /// ゲーム本体へ
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid toMainGameWithNotworking(string mineId, string enemyId, string roomName)
        {
            connectingText.text = "Game Start";
            await AppLib.LibFunction.FlashUIAsync(connectingText, this.GetCancellationTokenOnDestroy());
            loopExecuter.Push(eLoop.MainGameWithNetworking, new ParamMainGameWithNetwork { MineId = mineId, EnemyId = enemyId, RoomName = roomName, WithOther = loopParam.WithOther });
        }

        /// <summary>
        /// マッチング成功
        /// </summary>
        /// <param name="response"></param>
        public void OnMatchingSuccess(MatchingRequestSuccessResponse response)
        {
            Debug.Log(response.RoomName);
            roomName = response.RoomName;

            // ゲームへ移動
            toMainGameWithNotworking(response.MineId, response.EnemeyId, roomName).Forget();
        }
    }

    /// <summary>
    /// マッチングループ用Param
    /// </summary>
    public class ParamMatchingLoop : IBridgeLoopParam
    {
        public bool WithOther;
    }
}
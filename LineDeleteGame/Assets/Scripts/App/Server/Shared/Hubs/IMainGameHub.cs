using App.Shared.MessagePackObjects;
using MagicOnion;
using System.Threading.Tasks;

namespace App.Server.Hubs
{
    /// <summary>
    /// [Streaming] Client -> Server ... よってServer実装
    /// </summary>
    public interface IMainGameHub : IStreamingHub<IMainGameHub, IMainGameHubReceiver>
    {
        /// <summary>接続</summary>
        Task GameReadyAsync(GameReadyRequest request);

        /// <summary>ゲーム開始</summary>
        Task GameStartAsync(GameStartRequest request);

        /// <summary>入力受付</summary>
        Task InputAsync(GameInputRequest request);

        /// <summary>切断</summary>
        Task LeaveAsync();
    }

    /// <summary>
    /// [Streaming] Server -> Client ... よってClient実装
    /// </summary>
    public interface IMainGameHubReceiver
    {
        /// <summary>サーバー側のゲーム準備OK</summary>
        void OnGameReady(GameReadyResponse response);

        /// <summary>ゲーム開始</summary>
        void OnGameStart(GameStartResponse response);

        /// <summary>盤面などゲーム実行中の情報</summary>
        void OnGameProcess(GameProcessResponse response);
    }
}

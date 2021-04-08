using App.Shared.MessagePackObjects;
using MagicOnion;
using System.Threading.Tasks;

namespace App.Server.Hubs
{
    /// <summary>
    /// [Streaming] Client -> Server ... よってServer実装
    /// </summary>
    public interface IMatchingHub : IStreamingHub<IMatchingHub, IMatchingHubReceiver>
    {
        /// <summary>接続</summary>
        Task JoinAsync(MatchingJoinRequest request);

        /// <summary>切断</summary>
        Task LeaveAsync();
    }

    /// <summary>
    /// [Streaming] Server -> Client ... よってClient実装
    /// </summary>
    public interface IMatchingHubReceiver
    {
        /// <summary>commentマッチング成功</summary>
        void OnMatchingSuccess(MatchingRequestSuccessResponse response);
    }
}
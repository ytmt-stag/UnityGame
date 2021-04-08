using Cysharp.Threading.Tasks;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// Hub接続補助
    /// </summary>
    /// <typeparam name="TServer"></typeparam>
    /// <typeparam name="TClient"></typeparam>
    public class HubConnector<TServer, TClient> where TServer : IStreamingHub<TServer, TClient>
    {
        /// <summary>疎通時チャンネル</summary>
        private Grpc.Core.Channel channel = null;

        /// <summary>キャンセル処理は自身で管理</summary>
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>廃棄済み</summary>
        private bool isDisposed = false;

        /// <summary>Client -> Server</summary>
        public TServer ServerImpl { get; private set; } = default;

        /// <summary>Server -> Client</summary>
        private TClient ClientImpl = default;

        /// <summary>自分からDisconnectした</summary>
        private bool isSelfDisconnected = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public HubConnector(TClient receiver, string ipAddr, int port)
        {
            ClientImpl = receiver;

            channel = new Grpc.Core.Channel(ipAddr, port, ChannelCredentials.Insecure);
            channel.ShutdownToken.Register(() =>
            {
                Debug.Log("Call ShutdownToken From Register");
            });
        }

        /// <summary>
        /// 接続開始
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="ipAddr"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async UniTask ConnectStartAsync()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {   // 本当にサーバーに接続できるか確認
                    await channel.ConnectAsync(DateTime.UtcNow.AddSeconds(5));
                    Debug.Log("connecting to the server ... ");
                    ServerImpl = await StreamingHubClient.ConnectAsync<TServer, TClient>(channel, ClientImpl, cancellationToken: cancellationTokenSource.Token);
                    executeDisconnectEventWaiterAsync(ServerImpl).Forget();
                    Debug.Log("established connect");
                    break;
                }
                catch (TaskCanceledException)
                {
                }
                catch (OperationCanceledException e)
                {
                    Debug.LogWarning(e);
                    throw new OperationCanceledException();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                Debug.Log("Retry after 2 seconds");
                await UniTask.Delay(2 * 1000, cancellationToken: cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// 切断共通
        /// </summary>
        /// <returns></returns>
        public async UniTask DisposeConnectAsync()
        {
            if (!isDisposed)
            {
                await dispose();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// 接続切れ待機
        /// </summary>
        /// <param name="streamingClient"></param>
        private async UniTask executeDisconnectEventWaiterAsync(TServer streamingClient)
        {
            try
            {
                await streamingClient.WaitForDisconnect();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            finally
            {
                Debug.Log($"Disconnected from the server : {channel.State}");

                if (isSelfDisconnected)
                {   // 自ら切れた場合は再接続要求
                    await UniTask.Delay(2000, cancellationToken: cancellationTokenSource.Token);
                    await ConnectStartAsync();
                    isSelfDisconnected = false;
                }
            }
        }

        /// <summary>
        /// dispose処理
        /// </summary>
        /// <returns></returns>
        private async UniTask dispose()
        {
            // No exception should ever be thrown except in critical scenarios.
            // Unhandled exceptions during finalization will tear down the process.
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
            if (!isDisposed)
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    await onDisposing();
                }
                finally
                {
                    //When the task is ready, release the semaphore. It is vital to ALWAYS release the semaphore when we are ready, or else we will end up with a Semaphore that is forever locked.
                    //This is why it is important to do the Release within a try...finally clause; program execution may crash or take a different path, this way you are guaranteed execution
                    semaphoreSlim.Release();

                    // Ensure that the flag is set
                    isDisposed = true;
                }
            }

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// dispose本体
        /// </summary>
        /// <returns></returns>
        private async UniTask onDisposing()
        {
            cancellationTokenSource.Cancel();

            if (ServerImpl != null)
            {
                Debug.Log("ServerImpl DisposeAsync Start");
                await ServerImpl.DisposeAsync();
                Debug.Log("ServerImpl DisposeAsync Complete");
                ServerImpl = default;
            }
            if (channel != null)
            {
                Debug.Log("channel Shutdown Async Start");
                await channel.ShutdownAsync();
                Debug.Log("channel Shutdown Async Complete");
                channel = null;
            }

            ClientImpl = default;
        }
    }

}
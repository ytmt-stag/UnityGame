using Cysharp.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App.Server.Looper
{
    /// <summary>
    /// 全ユーザーのゲームの進行状態をここでHosting
    /// </summary>
    class MainGameLoopHostedService : IHostedService
    {
        private readonly ILogicLooperPool looperPool;
        private readonly ILogger logger;

        public MainGameLoopHostedService(ILogicLooperPool looperPool, ILogger<MainGameLoopHostedService> logger)
        {
            this.looperPool = looperPool ?? throw new ArgumentNullException(nameof(looperPool));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Example: Register update action immediately.
            _ = looperPool.RegisterActionAsync((in LogicLooperActionContext ctx) =>
            {
                if (ctx.CancellationToken.IsCancellationRequested)
                {
                    // If LooperPool begins shutting down, IsCancellationRequested will be `true`.
                    logger.LogInformation("LoopHostedService will be shutdown soon. The registered action is shutting down gracefully.");
                    return false;
                }

                return true;
            });

            // Example: Create a new world of life-game and register it into the loop.
            //MainGameServerLoop.CreateNew(looperPool, logger);

            logger.LogInformation($"LoopHostedService is started. (Loopers={looperPool.Loopers.Count}; TargetFrameRate={looperPool.Loopers[0].TargetFrameRate:0}fps)");

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("LoopHostedService is shutting down. Waiting for loops.");

            // Shutdown gracefully the LooperPool after 5 seconds.
            await looperPool.ShutdownAsync(TimeSpan.FromMilliseconds(4500));

            // Count remained actions in the LooperPool.
            var remainedActions = looperPool.Loopers.Sum(x => x.ApproximatelyRunningActions);
            logger.LogInformation($"{remainedActions} actions are remained in loop.");
        }
    }
}

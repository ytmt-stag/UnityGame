using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using AppLib;
using App.Shared.Common;

namespace App
{
    /// <summary>
    /// 一人プレイ
    /// </summary>
    public class MainGameAloneLoop : ILoop
    {
        /// <summary>盤面全体描画</summary>
        private BoardViewer viewer = null;

        /// <summary>入力系</summary>
        private IPlayInputUpdater input = null;

        /// <summary>State管理</summary>
        private StateController stateCtrl = new StateController();

        /// <summary>ゲーム開始したか</summary>
        public bool IsAlreadyStart { get; private set; } = false;

        /// <summary>
        /// 開始時初期化
        /// </summary>
        /// <returns></returns>
        async public override UniTask Enter(IBridgeLoopParam param)
        {
            // ボードのロード
            GameObject loadedObj = Instantiate(Resources.Load("Prefabs/Board/Board")) as GameObject;
            viewer = loadedObj != null ? loadedObj.GetComponent<BoardViewer>() : null;
            Assert.IsTrue(viewer != null, "Boardとしての機能がない");
            loadedObj.SetParentToLocalInitialize(gameObject);

            // 1人用に初期化
            input = new KeyPlayInput();

            // ゲームスタート
            GameStart(input);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// 終了時
        /// </summary>
        /// <returns></returns>
        async public override UniTask Exit(eLoop nextLoop)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// MonoBehaviour Update
        /// </summary>
        private void Update()
        {
            if (viewer == null || !IsAlreadyStart)
            {
                return;
            }

            // キー入力更新
            input.UpdateFrameProcess(Time.deltaTime);
            // State更新
            stateCtrl.Run(Time.deltaTime);

            // ゲームオーバー
            if (IsGameOver() && input.GetCurrentCommand() == ePlayCommand.Enter)
            {   // ゲームオーバーになったらタイトル戻す
                loopExecuter.Pop();
            }
        }

        /// <summary>
        /// Game Start
        /// </summary>
        public void GameStart(IPlayInputReader inputReader)
        {
            IMainGameLogicState.LogicInfo info;
            info.input = inputReader;
            info.threadLockObj = this;

            AlonePlayingState.AloneInfo ctrl;
            ctrl.viewer = viewer;

            IMainGameLogicState playing = new AlonePlayingState(info, ctrl);
            AlonePlayStartState start = new AlonePlayStartState(playing);
            stateCtrl.ReserveAddState(start);

            IsAlreadyStart = true;
        }

        /// <summary>
        /// GameOverか否か
        /// </summary>
        /// <returns></returns>
        public bool IsGameOver()
        {
            IState cur = stateCtrl.GetCurrentState();
            var gameOver = cur as AloneGameOverState;
            return gameOver != null;
        }
    }
}
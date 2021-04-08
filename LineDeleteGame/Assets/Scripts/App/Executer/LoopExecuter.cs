using AppLib;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;

namespace App
{
    /// <summary>
    /// Loop制御
    /// </summary>
    public class LoopExecuter : MonoBehaviourSingleton<LoopExecuter>
    {
        private class Param
        {
            /// <summary>Loop識別子</summary>
            public eLoop Current;
            /// <summary>Loopオブジェクト</summary>
            public ILoop Obj;
            /// <summary>Loop間受け渡し用パラメータ</summary>
            public IBridgeLoopParam BridgeParam;
        }

        /// <summary>LoopMain</summary>
        [SerializeField]
        private GameObject loopMain = null;

        /// <summary>Mainカメラ</summary>
        [SerializeField]
        private Camera mainCamera = null;

        /// <summary>Loop管理スタック</summary>
        private Stack<Param> stack = new Stack<Param>();

        /// <summary>現在のループ情報</summary>
        private Param param = null;

        /// <summary>シーンのフェード制御</summary>
        private SceneFader fader = null;

        /// <summary>フェード実行中</summary>
        public bool IsFading { get; private set; }

        /// <summary>
        /// Awake関数初期化
        /// </summary>
        protected override void onAwakeInit()
        {
            // Rootの子オブジェクト全削除
            foreach (Transform loop in loopMain.transform)
            {
                GameObject.Destroy(loop.gameObject);
            }
        }

        /// <summary>
        /// Start関数初期化
        /// </summary>
        protected override void onStartInit()
        {
            // Manager系(DIにしたい)
            fader = SceneFader.Instance;
        }

        /// <summary>
        /// 更新
        /// </summary>
        protected override void onUpdate()
        {
            if (enableBack())
            {
                if (param != null && !param.Obj.OnBack())
                {   // currentLoopでOnBackKeyに処理がない場合はデフォルトで前の画面に戻る
                    Pop();
                }
            }
        }

        /// <summary>
        /// Loop変更 / Stack Topを消して差し替える
        /// </summary>
        /// <param name="nextLoop"></param>
        public void Push(eLoop nextLoop, IBridgeLoopParam param = null)
        {
            Param parameter = new Param { Current = nextLoop, BridgeParam = param, Obj = null, };
            stack.Push(parameter);
            execAsync(parameter).Forget();
        }

        /// <summary>
        /// 前のLoopへ戻る
        /// </summary>
        public void Pop(IBridgeLoopParam overWriteParam = null)
        {
            // 排除
            stack.Pop();

            // 前のLoopへ戻る
            var param = stack.Peek();
            if (overWriteParam != null)
            {   // BridgeParamは上書きできるように
                param.BridgeParam = overWriteParam;
            }

            execAsync(param).Forget();
        }

        /// <summary>
        /// タイトル画面まで戻る
        /// </summary>
        /// <param name="overwiteParam"></param>
        public void ReturnToTitle(IBridgeLoopParam overwiteParam = null)
        {
            while (stack.Count > 1)
            {
                stack.Pop();
                var param = stack.Peek();
                if (param.Current == eLoop.Title)
                {
                    execAsync(param).Forget();
                    break;
                }
            }
        }

        /// <summary>
        /// Loop変更実行
        /// </summary>
        /// <returns></returns>
        private async UniTask execAsync(Param nextParam)
        {
            IsFading = true;
            await fader.FadeOutToBlack(this.GetCancellationTokenOnDestroy());
            IsFading = false;

            // 終了待ち / 破棄
            if (param != null && param.Obj != null)
            {
                await param.Obj.Exit(nextParam.Current);

                Destroy(param.Obj.gameObject);
                param = null;
            }

            // リセット処理
            mainCamera.transform.localPosition = new Vector3(0, 0, -10f);

            // 次のLoopを読み込んでスタックに積む
            var loopResource = LoopFunction.GetParamLoopResource(nextParam.Current);
            nextParam.Obj = loadLoop(loopResource.PrefabPath);

            // Param更新
            param = nextParam;

            // GC実行
            System.GC.Collect();

            // 読み込み待ち
            await param.Obj.Enter(param.BridgeParam);

            IsFading = true;
            await fader.FadeInToScene(this.GetCancellationTokenOnDestroy());
            IsFading = false;
        }

        /// <summary>
        /// Loopローディング
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private ILoop loadLoop(string path)
        {
            GameObject loadedGameObj = Instantiate(Resources.Load(path)) as GameObject;
            ILoop loadedLoop = loadedGameObj != null ? loadedGameObj.GetComponent<ILoop>() : null;

            Assert.IsTrue(loadedLoop != null, "無効なLoopが指定されている : " + path);

            // RootObjの子に設定
            loadedGameObj.SetParentToLocalInitialize(loopMain);

            // 必要な情報事前設定
            loadedLoop.SetLoopExecuter(this);

            return loadedLoop;
        }

        /// <summary>
        /// ボタン操作で戻れるか
        /// </summary>
        /// <returns></returns>
        private bool enableBack()
        {
            if (param == null || param.Obj == null || !param.Obj.EnableBack || IsFading)
            {
                return false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                return true;
            }
            return false;
        }
    }
}

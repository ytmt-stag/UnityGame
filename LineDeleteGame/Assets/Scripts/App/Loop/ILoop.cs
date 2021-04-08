using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace App
{
    /// <summary>
    /// Loop親クラス
    /// </summary>
    public abstract class ILoop : MonoBehaviour
    {
        /// <summary>LoopExecuter</summary>
        protected LoopExecuter loopExecuter { get; private set; }

        // ------------------------------
        // インターフェース
        // ------------------------------

        /// <summary>初期化関数</summary>
        public abstract UniTask Enter(IBridgeLoopParam param);

        /// <summary>後始末関数</summary>
        public abstract UniTask Exit(eLoop nextLoop);

        /// <summary>まえのLoopに戻れるか有効か (継承先で有効ならtrue)</summary>
        public virtual bool EnableBack { get { return true; } }

        // ------------------------------
        // 任意実装
        // ------------------------------

        /// <summary>
        /// MonoBehaviour Awake
        /// </summary>
        protected virtual void onAwakeInit()
        {
        }

        /// <summary>
        /// MonoBehaviour Start
        /// </summary>
        protected virtual void onStartInit()
        {
        }

        /// <summary>
        /// 戻るときの挙動 / overrideして挙動書き換えた際はreturn trueで実装 / baseはfalseにしとく。falseの場合はLoopManagerに一任する
        /// </summary>
        /// <returns></returns>
        public virtual bool OnBack()
        {
            return false;
        }

        /// <summary>
        /// MonoBehaviour Awake
        /// </summary>
        private void Awake()
        {
            onAwakeInit();
        }

        /// <summary>
        /// MonoBehaviour Start
        /// </summary>
        private void Start()
        {
            onStartInit();
        }

        /// <summary>
        /// LoopExecuter設定
        /// </summary>
        /// <param name="executer"></param>
        public void SetLoopExecuter(LoopExecuter executer)
        {
            loopExecuter = executer;
        }
    }

    /// <summary>
    /// Loop間パラメータ受け渡し
    /// </summary>
    public abstract class IBridgeLoopParam
    {
    }
}

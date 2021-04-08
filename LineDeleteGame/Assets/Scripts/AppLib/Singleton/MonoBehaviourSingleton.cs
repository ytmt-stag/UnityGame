using UnityEngine;
using UnityEngine.Assertions;

namespace AppLib
{
    /// <summary>
    /// Manager用 Singleton
    /// </summary>
    public abstract class MonoBehaviourSingletonAbst : MonoBehaviour
    {
        /// <summary>初期化</summary>
        public abstract void Initializing();

        /// <summary>破棄</summary>
        public abstract void DestroyForRestart();

        /// <summary>初期化に時間がかかるものは初期化終わるまでfalse / 初期化終了次第trueを</summary>
        public abstract bool IsReady();
    }

    /// <summary>
    /// Manager用 Singleton
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviourSingletonAbst where T : MonoBehaviourSingletonAbst
    {
        /// <summary>実体</summary>
        public static T Instance
        {
            get
            {   // Awakeで初期化されている想定
                Assert.IsTrue(instance != null, "instanceがNULLのまま使われている");
                return instance;
            }
        }

        /// <summary>実体持ってるかチェック</summary>
        public static bool HasInstance { get { return instance != null; } }

        /// <summary>実体 @ private</summary>
        private static T instance = null;

        /// <summary>
        /// Awake時初期化 / 継承任意
        /// </summary>
        protected virtual void onAwakeInit()
        {
        }

        /// <summary>
        /// Start時初期化
        /// </summary>
        protected virtual void onStartInit()
        {
        }

        /// <summary>
        /// MonoBehaviour Update相当
        /// </summary>
        protected virtual void onUpdate()
        {
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public sealed override void Initializing()
        {
            if (instance == null)
            {
                instance = GetComponent<T>();

                // 破棄されないように
                DontDestroyOnLoad(this.gameObject);
            }
        }

        /// <summary>
        /// リスタート用初期化
        /// </summary>
        public sealed override void DestroyForRestart()
        {
            if (instance != null)
            {
                Destroy(instance);
                instance = null;
            }
        }

        /// <summary>
        /// 初期化に時間がかかるものは初期化終わるまでfalse / 初期化終了次第trueを
        /// </summary>
        /// <returns></returns>
        public override bool IsReady()
        {
            return true;
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
        /// MonoBehaviour Update
        /// </summary>
        private void Update()
        {
            onUpdate();
        }
    }
}

using App.Shared.Common;
using AppLib;
using System.Collections;
using UnityEngine;

namespace App
{
    /// <summary>
    /// GameArrangement管理
    /// </summary>
    public class GameArrangement : MonoBehaviour
    {
        /// <summary>Manager系配列</summary>
        [SerializeField]
        private MonoBehaviourSingletonAbst[] singletonArray = null;

        /// <summary>
        /// 初期化実行
        /// </summary>
        /// <returns></returns>
        public IEnumerator starterEnumerator()
        {
            // FPSは30
            Application.targetFrameRate = SharedConstant.FPS;

            foreach (var singleton in singletonArray)
            {
                singleton.Initializing();
            }

            // 初期化待ち
            foreach (var singleton in singletonArray)
            {
                while (!singleton.IsReady())
                {
                    yield return null;
                }
            }

            yield break;
        }

        /// <summary>
        /// MonoBehaviour Awake
        /// </summary>
        private void Awake()
        {
            // 消させない
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// MonoBehaviour Start
        /// </summary>
        private IEnumerator Start()
        {
            // 初期化待ち
            yield return StartCoroutine(starterEnumerator());

            LoopExecuter.Instance.Push(eLoop.Title);
        }
    }

}

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace AppLib
{
    /// <summary>
    /// フェード実行用 / UI Canvasにつける想定
    /// </summary>
    public class SceneFader : MonoBehaviourSingleton<SceneFader>
    {
        /// <summary>デフォのフェード時間</summary>
        private const float DEFAULT_FADE_TIME = 0.5f;

        /// <summary>フェード画像 / AddComponent想定</summary>
        private Image fadeImage = null;

        /// <summary>
        /// Awake初期化
        /// </summary>
        protected override void onAwakeInit()
        {
            // Fadeオブジェクト生成
            var go = new GameObject();
            go.name = "Fade";
            go.SetParentToLocalInitialize(gameObject);

            // 初期設定
            fadeImage = go.AddComponent<Image>();
            fadeImage.rectTransform.sizeDelta = new Vector2(4000, 4000);
            fadeImage.color = new Color(0, 0, 0, 1);
        }

        /// <summary>
        /// フェードイン / シーンの始まり、暗転から明けるときに使う
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="beginByReset"></param>
        /// <param name="finishCallback"></param>
        async public UniTask FadeInToScene(CancellationToken cancellationToken, float duration = DEFAULT_FADE_TIME, bool beginByReset = true)
        {
            if (beginByReset)
            {
                fadeImage.SetAlpha(1f);
            }
            await LibFunction.FadeUIAsync(fadeImage, cancellationToken, 0, duration);
        }

        /// <summary>
        /// フェードアウト / シーンの最後、徐々に暗くするときに使う
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="beginByReset"></param>
        /// <param name="finishCallback"></param>
        async public UniTask FadeOutToBlack(CancellationToken cancellationToken, float duration = DEFAULT_FADE_TIME, bool beginByReset = true)
        {
            if (beginByReset)
            {
                fadeImage.SetAlpha(0f);
            }
            await LibFunction.FadeUIAsync(fadeImage, cancellationToken, 1, duration);
        }
    }
}

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace AppLib
{
    public class LibFunction
    {
        /// <summary>
        /// フェード制御
        /// </summary>
        /// <param name="endAlpha"></param>
        /// <param name="duration"></param>
        /// <param name="finishCallback"></param>
        /// <returns></returns>
        static public async UniTask FadeUIAsync(MaskableGraphic graphic, CancellationToken cancellationToken, float endAlpha, float duration)
        {
            // 有効化
            graphic.enabled = true;

            // 指定した値までEasing
            float startTime = Time.time;
            float startAlpha = graphic.color.a;
            while (true)
            {
                float ratio = (Mathf.Epsilon >= duration) ? 1.0f : (Time.time - startTime) / duration;
                graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, Mathf.Lerp(startAlpha, endAlpha, ratio));
                if (ratio >= 1.0f)
                {
                    break;
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: cancellationToken);
            }

            // fade image 無効化
            if (endAlpha <= 0f)
            {
                graphic.enabled = false;
            }
        }

        /// <summary>
        /// 適当に明滅させる
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="flashNum"></param>
        /// <param name="duration"></param>
        /// <param name="finishCallback"></param>
        /// <returns></returns>
        static public async UniTask FlashUIAsync(MaskableGraphic graphic, CancellationToken cancellationToken, int flashNum = 5, float duration = 0.06f)
        {
            for (int i = 0; i < flashNum; i++)
            {   // 適当にYOYO風に明滅
                await LibFunction.FadeUIAsync(graphic, cancellationToken, 0, duration);
                await LibFunction.FadeUIAsync(graphic, cancellationToken, 1, duration);
            }
        }
    }
}

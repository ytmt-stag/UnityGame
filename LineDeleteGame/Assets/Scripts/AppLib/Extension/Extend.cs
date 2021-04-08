using UnityEngine;
using UnityEngine.UI;

namespace AppLib
{
    /// <summary>
    /// 拡張メソッド / 定義しておくとすごく便利になる
    /// </summary>
    public static class Extend
    {
        /// <summary>
        /// 親セットして初期化
        /// </summary>
        /// <param name="ins"></param>
        /// <param name="parent"></param>
        public static void SetParentToLocalInitialize(this GameObject ins, GameObject parent)
        {
            setParentToLocalInitialize(ins, parent.transform);
        }

        /// <summary>
        /// Spriteのアルファ設定
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="alpha"></param>
        public static void SetAlpha(this Image image, float alpha)
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        }

        /// <summary>
        /// 親セットして初期化
        /// </summary>
        /// <param name="ins"></param>
        /// <param name="parent"></param>
        private static void setParentToLocalInitialize(GameObject ins, Transform parent)
        {
            ins.transform.SetParent(parent);
            ins.transform.localPosition = Vector3.zero;
            ins.transform.localScale = Vector3.one;
        }
    }
}

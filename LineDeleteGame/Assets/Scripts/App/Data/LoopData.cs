namespace App
{
    /// <summary>
    /// 各ループ定義
    /// </summary>
    public enum eLoop
    {
        /// <summary>未指定</summary>
        None,

        /// <summary>タイトル</summary>
        Title,
        /// <summary>メインゲーム(1人用)</summary>
        MainGameAlone,
        /// <summary>通信対戦</summary>
        MainGameWithNetworking,

        /// <summary>マッチング</summary>
        Matching,
    }

    /// <summary>
    /// LoopのPrefabリソース定義
    /// </summary>
    public class ParamLoopResource
    {
        /// <summary>Prefabリソースのパス</summary>
        public string PrefabPath { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="prefabPath"></param>
        public ParamLoopResource(string prefabPath)
        {
            PrefabPath = prefabPath;
        }
    }
}

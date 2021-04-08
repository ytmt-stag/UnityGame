using AppLib;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace App
{
    /// <summary>
    /// タイトル
    /// </summary>
    public class TitleLoop : ILoop
    {
        /// <summary>テキスト明滅回数</summary>
        public const int TWEEN_YOYO_NUM = 5;

        /// <summary>1人プレイ用促しテキスト</summary>
        [SerializeField]
        private TextMeshProUGUI aloneGame = null;

        /// <summary>通信対戦用促しテキスト</summary>
        [SerializeField]
        private TextMeshProUGUI networkingGame = null;

        /// <summary>遷移実行中</summary>
        private bool isTransiting = false;

        /// <summary>アプリを終わらせる処理を入れたらtrueにする</summary>
        public override bool EnableBack => false;

        /// <summary>
        /// 開始時初期化
        /// </summary>
        /// <returns></returns>
        async public override UniTask Enter(IBridgeLoopParam param)
        {   // warning回避…
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// 終了時
        /// </summary>
        /// <returns></returns>
        async public override UniTask Exit(eLoop nextLoop)
        {   // warning回避…
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// MonoBehavour Update
        /// </summary>
        private void Update()
        {
            if (isTransiting)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {   // 1人プレイ(オフライン)
                transitStart(aloneGame, eLoop.MainGameAlone).Forget();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {   // 工事中(二人で対戦)
                transitStart(networkingGame, eLoop.Matching, new ParamMatchingLoop { WithOther = true }).Forget();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {   // 工事中(1人でネットワーク接続 / スコア更新用)
                transitStart(networkingGame, eLoop.Matching, new ParamMatchingLoop { WithOther = false }).Forget();
            }
        }

        /// <summary>
        /// テキスト明滅させて次のLoopへ遷移
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="loopId"></param>
        /// <returns></returns>
        async private UniTask transitStart(TextMeshProUGUI txt, eLoop loop, IBridgeLoopParam param = null)
        {
            isTransiting = true;

            await LibFunction.FlashUIAsync(txt, this.GetCancellationTokenOnDestroy());
            await UniTask.Delay(200, cancellationToken: this.GetCancellationTokenOnDestroy());

            loopExecuter.Push(loop, param);
        }
    }
}

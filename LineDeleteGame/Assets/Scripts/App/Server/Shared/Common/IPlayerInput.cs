using System.Collections;
using System.Collections.Generic;

namespace App.Shared.Common
{
    /// <summary>
    /// ゲーム中の入力 / 読み込み専用
    /// </summary>
    public interface IPlayInputReader
    {
        /// <summary>
        /// 現在のコマンド取得
        /// </summary>
        /// <returns></returns>
        ePlayCommand GetCurrentCommand();
    }

    /// <summary>
    /// ゲーム中の入力 / Updateする用
    /// </summary>
    public interface IPlayInputUpdater : IPlayInputReader
    {
        /// <summary>
        /// 毎フレームの更新
        /// </summary>
        /// <param name="_dt"></param>
        void UpdateFrameProcess(float dt);
    }
}
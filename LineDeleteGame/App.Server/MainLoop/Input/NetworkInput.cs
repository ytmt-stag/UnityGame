using App.Shared.Common;

namespace App.Server.Looper
{
    public class NetworkInput : IPlayInputUpdater
    {
        private ePlayCommand command = ePlayCommand.None;

        /// <summary>
        /// 現在のコマンド取得 / 取得後初期化するので副作用あり
        /// </summary>
        /// <returns></returns>
        public ePlayCommand GetCurrentCommand()
        {   // 一回取得した後は無効
            ePlayCommand ret = command;
            command = ePlayCommand.None;
            return ret;
        }

        /// <summary>
        /// 毎フレームの更新
        /// </summary>
        /// <param name="dt"></param>
        public void UpdateFrameProcess(float dt)
        {   // 空実装
        }

        /// <summary>
        /// 値を変えずにCurrentCommand取得
        /// </summary>
        /// <returns></returns>
        public ePlayCommand PeekCurrentCommand()
        {
            return command;
        }

        /// <summary>
        /// アップデート
        /// </summary>
        public void SetCurrentCommand(ePlayCommand com)
        {
            command = com;
        }
    }
}

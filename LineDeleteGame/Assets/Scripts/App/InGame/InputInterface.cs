using UnityEngine;
using App.Shared.Common;

namespace App
{
    /// <summary>
    /// キー操作によるinput
    /// </summary>
    public class KeyPlayInput : IPlayInputUpdater
    {
        /// <summary>最初の反応時間</summary>
        public const float DURATION_FIRST_GEAR = 0.4f;
        /// <summary>押しっぱなしの反応時間</summary>
        public const float DURATION_SECOND_GEAR = 0.075f;

        /// <summary>キー状態</summary>
        struct KeyState
        {
            /// <summary>押されているコマンド</summary>
            public ePlayCommand command;
            /// <summary>押されている時間</summary>
            public float duration;
            /// <summary>加速するか</summary>
            public bool isAccelerate;

            /// <summary>入力タイプ</summary>
            public eInputType inputType
            {
                get
                {
                    switch (command)
                    {
                        case ePlayCommand.Down:
                        case ePlayCommand.Left:
                        case ePlayCommand.Right:
                        case ePlayCommand.Rotate:
                            return eInputType.Repeat;
                        case ePlayCommand.DownImmediately:
                        case ePlayCommand.PauseAndResume:
                        case ePlayCommand.Enter:
                        case ePlayCommand.Cancel:
                            return eInputType.Down;
                    }
                    return eInputType.None;
                }
            }

            /// <summary>
            /// 初期化
            /// </summary>
            public void Initialize()
            {
                command = ePlayCommand.None;
                duration = 0f;
                isAccelerate = false;
            }
        }

        /// <summary>現在のキー状態</summary>
        private KeyState current;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KeyPlayInput()
        {
            current.Initialize();
        }

        /// <summary>
        /// 現在のコマンド
        /// </summary>
        /// <returns></returns>
        public ePlayCommand GetCurrentCommand()
        {
            if (current.command != ePlayCommand.None && current.duration == 0f)
            {
                return current.command;
            }
            return ePlayCommand.None;
        }

        /// <summary>
        /// 毎フレーム更新処理
        /// </summary>
        /// <param name="dt"></param>
        public void UpdateFrameProcess(float dt)
        {
            // 現在のキー情報取得
            ePlayCommand command = getInputCommand();

            if (command == ePlayCommand.None)
            {   // 特に何も押されてない
                if (current.command != ePlayCommand.None)
                {   // 何か設定されてたらリセットしとく
                    current.Initialize();
                }
            }
            else if (command == current.command)
            {   // 現在と同じボタンが押された
                current.duration += dt;
                switch (current.inputType)
                {
                    case eInputType.Press:
                        // 押しっぱなしなので常に判定
                        current.duration = 0f;
                        break;
                    case eInputType.Repeat:
                        {   // 判定に遊びを作る
                            float maxDuration = current.isAccelerate ? DURATION_SECOND_GEAR : DURATION_FIRST_GEAR;
                            if (current.duration >= maxDuration)
                            {
                                current.duration = 0f;
                                current.isAccelerate = true;
                            }
                        }
                        break;
                    case eInputType.Down:
                        // 初回だけ反応するので何もしない
                        break;
                }
            }
            else
            {   // 初めて押された
                current.command = command;
                current.duration = 0f;
                current.isAccelerate = false;
            }
        }

        /// <summary>
        /// キー入力取得
        /// </summary>
        /// <returns></returns>
        private ePlayCommand getInputCommand()
        {
            ePlayCommand command = ePlayCommand.None;
            if (Input.GetKey(KeyCode.RightArrow))
            {
                command = ePlayCommand.Right;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                command = ePlayCommand.Left;
            }
            else if (Input.GetKey(KeyCode.UpArrow))
            {
                command = ePlayCommand.Rotate;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                command = ePlayCommand.Down;
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                command = ePlayCommand.DownImmediately;
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                command = ePlayCommand.PauseAndResume;
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                command = ePlayCommand.Enter;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                command = ePlayCommand.Cancel;
            }

            return command;
        }
    }

}
using App.Shared.MessagePackObjects;
using System;
using System.Collections.Generic;

namespace App.Shared.Common
{
    /// <summary>
    /// ゲームロジック / 継承先で各種アクションを実装 / オフライン時はクライアント、オンライン時はサーバーゲームループ側で使用
    /// </summary>
    public abstract class IMainGameLogicState : IState
    {
        /// <summary>
        /// ロジック側情報
        /// </summary>
        public struct LogicInfo
        {
            /// <summary>入力系</summary>
            public IPlayInputReader input;

            public object threadLockObj;
        }

        /// <summary>ボード情報</summary>
        protected BoardInfo boardInfo { get; }

        /// <summary>スコア情報</summary>
        protected ScoreInfo scoreInfo { get; }

        /// <summary>入力系</summary>
        protected IPlayInputReader input = null;

        /// <summary>現在落ちているブロックのステータス</summary>
        protected BlockStatus current = new BlockStatus();

        /// <summary>設置を試みているブロックのステータス</summary>
        protected BlockStatus attempt = new BlockStatus();

        /// <summary>次にくるブロック管理</summary>
        protected BlockStatus[] nextBlocks = new BlockStatus[SharedConstant.SHOW_NEXT_BLOCK_NUM];

        /// <summary>落とすスピード</summary>
        protected float speed = 0.4f;

        /// <summary>経過時間</summary>
        protected float duration = 0;

        /// <summary>乱数生成器</summary>
        protected Random rand = null;

        /// <summary>プレイ中か(仮フラグ)</summary>
        private bool isPlaying = true;

        /// <summary>落ち終わりフラグ</summary>
        private bool isEndOfDown = false;

        /// <summary>消えるだろう</summary>
        private List<short> willDeleteLine { get; } = new List<short>(10);

        /// <summary>通信対戦時、ボード情報書き換えるときのスレッドロック用</summary>
        private object threadLockObj { get; }

        /// <summary>
        /// ブロック落ち始め
        /// </summary>
        protected abstract void onBlockDropStart();

        /// <summary>
        /// ポーズ時実行されるので継承先でなにか
        /// </summary>
        protected abstract void onPause();

        /// <summary>
        /// 落ち終わってブロックが消えるとき
        /// </summary>
        protected abstract void onDeleteLine();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_render"></param>
        /// <param name="_info"></param>
        public IMainGameLogicState(LogicInfo ctrlInfo)
        {
            input = ctrlInfo.input;
            threadLockObj = ctrlInfo.threadLockObj;

            lock (threadLockObj)
            {   // boardInfoはマルチスレッドで非同期にアクセスするので内部のnew割り当てはlockする必要がある
                boardInfo = new BoardInfo();
                scoreInfo = new ScoreInfo();
            }
        }

        /// <summary>
        /// State入った
        /// </summary>
        public override void Enter()
        {
            Reset();
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="_dt"></param>
        /// <returns></returns>
        public override sealed bool Update(float dt)
        {
            if (!isPlaying)
            {
                return false;
            }

            ePlayCommand command = input.GetCurrentCommand();

            BlockStatus attemptPut = current;
            switch (command)
            {
                case ePlayCommand.Rotate:
                    attemptPut.RotateNum++;
                    break;
                case ePlayCommand.Right:
                    attemptPut.Pos.x++;
                    break;
                case ePlayCommand.Left:
                    attemptPut.Pos.x--;
                    break;
                case ePlayCommand.Down:
                case ePlayCommand.DownImmediately:
                    duration = speed;
                    break;
                case ePlayCommand.PauseAndResume:
                    onPause();
                    return true;
            }

            lock (threadLockObj)
            {   // 通信対戦時にここで書き換えている値を参照するのでlock
                mutableUpdate(attemptPut, command, dt);
            }

            return true;
        }

        /// <summary>
        /// 終了 / 大抵ゲームオーバー
        /// </summary>
        /// <returns></returns>
        public override IState Exit()
        {
            return null;
        }

        /// <summary>
        /// 戻ってきたとき / Baseから乗っ取り
        /// </summary>
        /// <param name="_prevState"></param>
        public override sealed void CalledOnResume(IState prevState)
        {
            if (isEndOfDown)
            {
                isEndOfDown = false;

                // ブロック消すStateから戻ってきたら次のブロック
                // ただし、次のブロックを置けなかったらいわゆるゲームオーバー
                initializeCurrentStatus();
                if (!boardInfo.PutBlock(current))
                {   // ゲームオーバーへ遷移
                    isPlaying = false;
                }
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public virtual void Reset()
        {
            // 自分のメンバ初期化
            isPlaying = true;
            duration = 0;
            rand = new Random();

            // 初期化
            boardInfo.Initialize();
            scoreInfo.Reset();
            willDeleteLine.Clear();

            // 予め2個積んでおく
            for (int i = 0; i < SharedConstant.SHOW_NEXT_BLOCK_NUM; ++i)
            {
                nextBlocks[i] = generateInitBlock();
            }

            // 初期化
            initializeCurrentStatus();

            // 配置と描画
            boardInfo.PutBlock(current);
        }

        /// <summary>
        /// 初期ブロック生成
        /// </summary>
        /// <returns></returns>
        private void initializeCurrentStatus()
        {
            // 現在位置 / 設置予定位置 / 両方初期化
            current = nextBlocks[0];
            attempt = nextBlocks[0];

            // 詰める
            nextBlocks[0] = nextBlocks[1];
            nextBlocks[1] = generateInitBlock();

            onBlockDropStart();
        }

        /// <summary>
        /// 初期ブロック生成
        /// </summary>
        /// <returns></returns>
        private BlockStatus generateInitBlock()
        {
            BlockStatus status;
            status.Pos.x = 5;
            status.Pos.y = 22;
            status.Type = (short)rand.Next(1, SharedConstant.BLOCK_TYPE_NUM);
            status.RotateNum = 0;
            status.SettingDataIdx = status.Type;

            return status;
        }

        /// <summary>
        /// 書き換え前提の更新 / lock必須
        /// </summary>
        /// <param name="_attemptPut"></param>
        /// <param name="command"></param>
        /// <param name="_dt"></param>
        private void mutableUpdate(BlockStatus attemptPut, ePlayCommand command, float dt)
        {
            current = operatePanel(current, attemptPut);

            // 一定時間超えたらブロックを一段下へ
            bool willLinesDelete = false;
            duration += dt;
            if (duration >= speed)
            {
                willLinesDelete = downBlock(command == ePlayCommand.DownImmediately);
                duration = 0;
            }

            if (willLinesDelete)
            {   // 消える行がある場合、downStateへ移動
                isEndOfDown = true;
                onDeleteLine();
            }
            else
            {   // 最下層の位置を描画
                attempt = drawLowestBlock(current, attempt);
            }
        }

        /// <summary>
        /// ブロックを一段下へ
        /// </summary>
        /// <returns></returns>
        private bool downBlock(bool immediately)
        {
            // 常時は1マス分 / immediately指定時は置けなくなるまでリピート
            bool isPuttable = true;
            do
            {   // 一旦現在位置を削除
                boardInfo.DeleteBlock(current);

                // 現在位置を一つ下げた状態でタイル情報設定しなおし
                current.Pos.y--;
                if (!boardInfo.PutBlock(current))
                {
                    isPuttable = false;
                }
            } while (immediately && isPuttable);

            // 置けなかった場合、次のブロックへ遷移
            bool willDelete = false;
            if (!isPuttable)
            {
                // 置けなかった分を元に戻す
                current.Pos.y++;
                boardInfo.PutBlock(current);

                // そろった行を消す
                BoardInfo.SearchWillDeleteLineNumber(willDeleteLine, boardInfo.Board);
                if (willDeleteLine.Count > 0)
                {
                    willDelete = true;
                    scoreInfo.AddScoreByDeleteLines(willDeleteLine.Count);
                }
                else
                {
                    initializeCurrentStatus();
                    if (!boardInfo.PutBlock(current))
                    {
                        isPlaying = false;
                    }
                }
            }

            // 結果を返す
            return willDelete;
        }

        /// <summary>
        /// ブロック操作をした時の更新
        /// </summary>
        /// <param name="currentStatus"></param>
        /// <param name="attemptPut"></param>
        private BlockStatus operatePanel(BlockStatus currentStatus, BlockStatus attemptPut)
        {
            // 左右上下回転、全て更新がなければ何もしない
            bool updateDir = (attemptPut.Pos.x != current.Pos.x) || (attemptPut.Pos.y != current.Pos.y);
            bool updateRotate = attemptPut.RotateNum != current.RotateNum;
            if (!updateDir && !updateRotate)
            {   // 更新なし
                return currentStatus;
            }

            // 現在位置は一旦削除
            boardInfo.DeleteBlock(currentStatus);

            bool updateCurrent = false;
            // 方向が変わったとき
            if (boardInfo.PutBlock(attemptPut))
            {   // 位置更新できたらこちらの座標も更新後の座標へ
                updateCurrent = true;
            }
            else if (updateRotate)
            {   // 回転時に失敗した場合、中心位置を移動して回転を試みる
                GridPos attemptRotatePos = attemptRotateMovingCenter(attemptPut);
                updateCurrent = (attemptPut.Pos.x != attemptRotatePos.x) || (attemptPut.Pos.y != attemptRotatePos.y);
                attemptPut.Pos = attemptRotatePos;
            }

            if (updateCurrent)
            {   // attemptを狙った位置に更新できたら、currentも変えておく
                currentStatus = attemptPut;
            }
            else
            {   // 更新できなかったら現在位置にしとく
                boardInfo.PutBlock(currentStatus);
            }

            return currentStatus;
        }

        /// <summary>
        /// 中心位置を移動して回転を試みる
        /// </summary>
        /// <param name="attemptPut"></param>
        /// <returns></returns>
        private GridPos attemptRotateMovingCenter(BlockStatus attemptPut)
        {
            GridPos originPos = attemptPut.Pos;

            // 回転時に失敗した場合は左右にずらしてみる
            attemptPut.Pos.x = (short)(originPos.x + 1);
            if (BoardInfo.GetBoardTypeFromOriginSentinel(originPos.x - 1, originPos.y, boardInfo.Board) != 0 && boardInfo.PutBlock(attemptPut))
            {
                return attemptPut.Pos;
            }

            attemptPut.Pos.x = (short)(originPos.x - 1);
            if (BoardInfo.GetBoardTypeFromOriginSentinel(originPos.x + 1, originPos.y, boardInfo.Board) != 0 && boardInfo.PutBlock(attemptPut))
            {
                return attemptPut.Pos;
            }

            // Lineブロックでなければここで終了
            if (!attemptPut.IsLineBlock)
            {
                return originPos;
            }

            // Lineブロックは±2も試す
            attemptPut.Pos.x = (short)(originPos.x + 2);
            if (BoardInfo.GetBoardTypeFromOriginSentinel(originPos.x - 1, originPos.y, boardInfo.Board) != 0 && boardInfo.PutBlock(attemptPut))
            {
                return attemptPut.Pos;
            }

            attemptPut.Pos.x = (short)(originPos.x - 2);
            if (BoardInfo.GetBoardTypeFromOriginSentinel(originPos.x + 1, originPos.y, boardInfo.Board) != 0 && boardInfo.PutBlock(attemptPut))
            {
                return attemptPut.Pos;
            }

            // ここまで来てダメならもうダメ
            return originPos;
        }

        /// <summary>
        /// 最下層のブロック位置計算
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private BlockStatus drawLowestBlock(BlockStatus current, BlockStatus attempt)
        {
            // 一旦現在位置と予想位置を削除
            boardInfo.DeleteBlock(attempt);
            boardInfo.DeleteBlock(current);

            // タイプのみ配置予定ブロックに変更(設定データはそのままでよい)
            BlockStatus attemptDown = this.current;
            attemptDown.Type = SharedConstant.BLOCK_TYPE_ATTEMPT;

            // 現在位置をどんどん下げてタイル情報設定しなおし
            while (true)
            {
                attemptDown.Pos.y--;
                if (!boardInfo.PutBlock(attemptDown))
                {   // 下げた位置におけなかったら1個上に戻す / attempt部分を再配置して終了
                    attemptDown.Pos.y++;
                    boardInfo.PutBlock(attemptDown);
                    break;
                }
                else
                {   // 置けたなら即消し
                    boardInfo.DeleteBlock(attemptDown);
                }
            }

            // 元々のcurrentを上書き
            boardInfo.PutBlock(current);

            return attemptDown;
        }
    }
}
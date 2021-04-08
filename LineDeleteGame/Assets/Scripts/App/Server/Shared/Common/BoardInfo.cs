using System.Collections.Generic;
using App.Shared.MessagePackObjects;

namespace App.Shared.Common
{
    /// <summary>
    /// ゲーム盤上情報
    /// 外部からは[0～width-1, 0～height-1]として盤にアクセス
    /// 内部処理は番兵処理しているので[1～width, 1～height]で盤にアクセス
    /// </summary>
    public class BoardInfo
    {
        /// <summary>ゲーム盤</summary>
        private List<short> board = null;

        /// <summary>消えるであろう行番号(毎回newしないように)</summary>
        private List<short> willDeleteLineNumbers = null;

        /// <summary>ボード情報(読み取り専用)</summary>
        public IReadOnlyList<short> Board { get { return board.AsReadOnly(); } }

        /// <summary>
        /// 回転位置計算
        /// </summary>
        /// <param name="_relativePos"></param>
        /// <param name="_blockRotatableNum"></param>
        /// <param name="_currentRotateNum"></param>
        /// <returns></returns>
        public static GridPos CalcRelativeRotatePos(GridPos relativePos, int blockRotatableNum, int currentRotateNum)
        {
            GridPos RotatePos = new GridPos(relativePos.x, relativePos.y);
            int rotateNum = currentRotateNum % blockRotatableNum;
            for (int r = 0; r < rotateNum; r++)
            {   // 時計回りに回したいので270度回転
                // |cos270 -sin270| |x| -> | 0  1| |x| = (y, -x)  
                // |sin270  cos270| |y|    |-1  0| |y|
                short nx = RotatePos.x, ny = RotatePos.y;
                RotatePos.x = ny;
                RotatePos.y = (short)-nx;
            }

            return RotatePos;
        }

        /// <summary>
        /// 外部からは[0～width-1, 0～height-1]でアクセスできるように補正 / 番兵を意識しなくてよいように
        /// 番兵補正してるので内部で使用するとバグる
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <returns></returns>
        public static int GetBoardTypeFromOriginZero(int x, int y, IReadOnlyList<short> board)
        {   // 番兵考慮して返却
            int idx = calcIdx(x + 1, y + 1);
            return board[idx];
        }

        /// <summary>
        /// 外部からは[0～width-1, 0～height-1]でアクセスできるように補正 / 番兵を意識しなくてよいように
        /// 番兵補正してるので内部で使用するとバグる
        /// </summary>
        /// <param name="_pos"></param>
        /// <returns></returns>
        public static int GetBoardTypeFromOriginZero(GridPos pos, IReadOnlyList<short> board)
        {
            return GetBoardTypeFromOriginZero(pos.x, pos.y, board);
        }

        /// <summary>
        /// 番兵込のインデックスでアクセス
        /// </summary>
        /// <param name="_pos"></param>
        /// <returns></returns>
        public static int GetBoardTypeFromOriginSentinel(int x, int y, IReadOnlyList<short> board)
        {
            int idx = calcIdx(x, y);
            return board[idx];
        }

        /// <summary>
        /// ブロックが存在するかチェック(設置予定ブロックは加味しない)
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <returns></returns>
        public static bool HasBlock(int x, int y, IReadOnlyList<short> board)
        {
            int idx = calcIdx(x, y);
            return board[idx] != SharedConstant.BLOCK_TYPE_NULL && board[idx] != SharedConstant.BLOCK_TYPE_ATTEMPT;
        }

        /// <summary>
        /// ブロックが存在するかチェック(設置予定ブロックは加味しない)
        /// </summary>
        /// <param name="_pos"></param>
        /// <returns></returns>
        public static bool HasBlock(GridPos pos, IReadOnlyList<short> board)
        {
            return HasBlock(pos.x, pos.y, board);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_blockData"></param>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        public BoardInfo()
        {
            // 消すであろう行数(capacity指定しとく)
            willDeleteLineNumbers = new List<short>(10);

            // 番兵込でボード情報初期化
            board = new List<short>(SharedConstant.BOARD_ARRAY_SIZE);
            Initialize();
        }

        /// <summary>
        /// 盤を初期化
        /// </summary>
        public void Initialize()
        {
            willDeleteLineNumbers.Clear();
            board.Clear();
            for (int y = 0; y < SharedConstant.HEIGHT_WITH_SENTINEL; y++)
            {
                for (int x = 0; x < SharedConstant.WIDTH_WITH_SENTINEL; x++)
                {
                    // 境界は1, それ以外はブロックなしで初期化
                    short boardType = 0;
                    if (x == 0 || x == SharedConstant.WIDTH_WITH_SENTINEL - 1 || y == 0)
                    {   // ボード境界(番兵)は何かブロック配置済みとして初期化
                        boardType = 1;
                    }

                    // 追加
                    board.Add(boardType);
                }
            }
        }

        /// <summary>
        /// 指定ブロック配置
        /// </summary>
        /// <param name="_attemptPut"></param>
        /// <returns></returns>
        public bool PutBlock(BlockStatus attemptPut)
        {
            if (!IsBlockPuttable(attemptPut))
            {   // 更新可能かチェック事前チェック
                return false;
            }

            int statIdx = calcIdx(attemptPut.Pos);
            short setType = attemptPut.Type;

            // (0,0)座標のブロック更新
            board[statIdx] = setType;

            // 相対座標分のブロック更新
            for (int i = 0; i < SharedConstant.RELATIVE_BLOCK_NUM; i++)
            {
                BlockSettingData settingData = SharedConstant.BLOCK_DATA[attemptPut.SettingDataIdx];
                GridPos diffPos = CalcRelativeRotatePos(settingData.RelativePos[i], settingData.RotatableNum, attemptPut.RotateNum);

                int expectedIdx = calcIdx(attemptPut.Pos.x + diffPos.x, attemptPut.Pos.y + diffPos.y);
                board[expectedIdx] = setType;
            }

            return true;
        }

        /// <summary>
        /// 指定ブロック削除
        /// </summary>
        /// <param name="_stat"></param>
        public void DeleteBlock(BlockStatus stat)
        {
            int statIdx = calcIdx(stat.Pos);

            // (0,0)座標のブロック更新
            board[statIdx] = SharedConstant.BLOCK_TYPE_NULL;

            // 相対座標分のブロック更新
            for (int i = 0; i < SharedConstant.RELATIVE_BLOCK_NUM; i++)
            {
                BlockSettingData settingData = SharedConstant.BLOCK_DATA[stat.SettingDataIdx];
                GridPos diffPos = CalcRelativeRotatePos(settingData.RelativePos[i], settingData.RotatableNum, stat.RotateNum);

                int expectedIdx = calcIdx(stat.Pos.x + diffPos.x, stat.Pos.y + diffPos.y);
                board[expectedIdx] = SharedConstant.BLOCK_TYPE_NULL;
            }
        }

        /// <summary>
        /// 消そうと思った行番号取得
        /// </summary>
        /// <param name="_willDelete"></param>
        public static void SearchWillDeleteLineNumber(List<short> ret, IReadOnlyList<short> board)
        {
            ret.Clear();

            for (short y = 1; y < 24; ++y)
            {
                bool erase = true;
                for (short x = 1; x <= SharedConstant.BOARD_WIDTH; ++x)
                {
                    if (!HasBlock(x, y, board))
                    {   // 消そうとしている行に空き領域がある場合、それは削除対象外
                        erase = false;
                    }
                }

                if (erase)
                {   // 消した行返す
                    ret.Add(y);
                }
            }
        }

        /// <summary>
        /// 行消せるか判定のみ
        /// </summary>
        /// <param name="_board"></param>
        /// <returns></returns>
        public static bool HasDeleteLine(IReadOnlyList<short> board)
        {
            for (short y = 1; y < 24; ++y)
            {
                bool erase = true;
                for (short x = 1; x <= SharedConstant.BOARD_WIDTH; ++x)
                {
                    if (!HasBlock(x, y, board))
                    {   // 消そうとしている行に空き領域がある場合、それは削除対象外
                        erase = false;
                    }
                }

                if (erase)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// そろった行を削除
        /// </summary>
        /// <returns>消した行「数」(消した行ではない)</returns>
        public int DeleteLine()
        {
            int deleteCount = 0;
            for (int y = 1; y < 24; ++y)
            {
                bool erase = true;
                for (int x = 1; x <= SharedConstant.BOARD_WIDTH; ++x)
                {
                    if (!HasBlock(x, y, board))
                    {   // 消そうとしている行に空き領域がある場合、それは削除対象外
                        erase = false;
                        break;
                    }
                }

                if (erase)
                {
                    for (int k = y; k < 24; ++k)
                    {
                        for (int i = 1; i <= SharedConstant.BOARD_WIDTH; i++)
                        {
                            int curIdx = calcIdx(i, k);
                            int upIdx = calcIdx(i, k + 1);
                            board[curIdx] = board[upIdx];
                        }
                    }
                    // 複数行消したときに、一段ぬかしになってしまうので今いるy座標をキープ
                    --y;

                    // 消した行カウント増やしとく
                    ++deleteCount;
                }
            }

            return deleteCount;
        }

        /// <summary>
        /// 指定ブロックが配置可能か
        /// </summary>
        /// <param name="_stat"></param>
        /// <returns></returns>
        public bool IsBlockPuttable(BlockStatus stat)
        {
            if (HasBlock(stat.Pos, board))
            {   // 何かしらブロック配置済み
                return false;
            }

            for (int i = 0; i < SharedConstant.RELATIVE_BLOCK_NUM; i++)
            {
                BlockSettingData blockData = SharedConstant.BLOCK_DATA[stat.SettingDataIdx];
                GridPos diffPos = CalcRelativeRotatePos(blockData.RelativePos[i], blockData.RotatableNum, stat.RotateNum);

                // 回転後の座標に空き領域がなければそこにはブロックは置けない
                if (HasBlock(stat.Pos.x + diffPos.x, stat.Pos.y + diffPos.y, board))
                {
                    return false;
                }
            }

            // 更新可能
            return true;
        }

        /// <summary>
        /// 番兵込のIndex取得
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <returns></returns>
        private static int calcIdx(int x, int y)
        {
            return y * SharedConstant.WIDTH_WITH_SENTINEL + x;
        }

        /// <summary>
        /// 番兵込のIndex取得
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static int calcIdx(GridPos pos)
        {
            return calcIdx(pos.x, pos.y);
        }
    }
}

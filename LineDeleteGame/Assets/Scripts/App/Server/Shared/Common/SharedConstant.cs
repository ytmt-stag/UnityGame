using App.Shared.MessagePackObjects;
using System.Collections.Generic;

namespace App.Shared.Common
{
    /// <summary>ゲームプレイ中の操作コマンド</summary>
    public enum ePlayCommand
    {
        None = 0,

        /// <summary>回転</summary>
        Rotate,
        /// <summary>左</summary>
        Left,
        /// <summary>右</summary>
        Right,
        /// <summary>下</summary>
        Down,
        /// <summary>即刻下へ</summary>
        DownImmediately,

        /// <summary>決定</summary>
        Enter,
        /// <summary>キャンセル</summary>
        Cancel,
        /// <summary>一時停止/再開</summary>
        PauseAndResume,

        Max,
    }

    /// <summary>操作タイプ</summary>
    public enum eInputType
    {
        None,

        /// <summary>一回押した後、しばらく押し続けた後押しっぱなしになる</summary>
        Repeat,
        /// <summary>一回押したら終わり</summary>
        Down,
        /// <summary>押しっぱなし</summary>
        Press,
    }

    /// <summary>
    /// ブロック設定情報
    /// </summary>
    public class BlockSettingData
    {
        /// <summary>何回回転できるか</summary>
        public readonly int RotatableNum = 0;
        /// <summary>相対的なポジション</summary>
        public readonly GridPos[] RelativePos = new GridPos[3];

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_rotateNum">何回回転できるか</param>
        /// <param name="_pos">相対的な位置</param>
        public BlockSettingData(int rotateNum, GridPos[] pos)
        {
            RotatableNum = rotateNum;
            for (int i = 0; i < RelativePos.Length; i++)
            {
                RelativePos[i] = pos[i];
            }
        }
    }

    /// <summary>
    /// 定数一覧
    /// </summary>
    public class SharedConstant
    {
        /// <summary>gRPC通信の接続アドレス</summary>
        public const string GRPC_CONNECT_ADDRESS = "192.168.1.14";

        /// <summary>gRPC通信の接続ポート</summary>
        public const int GRPC_CONNECT_PORT = 5000;

        /// <summary>最大同時プレイ数</summary>
        public const int MAX_PLAYER_NUM_IN_GAME = 2;

        /// <summary>自分</summary>
        public const int PLAYER_MINE_IDX = 0;

        /// <summary>対戦相手</summary>
        public const int PLAYER_ENEMY_IDX = 1;

        /// <summary>FPSは30固定</summary>
        public const short FPS = 30;

        /// <summary>ゲーム盤幅</summary>
        public const short BOARD_WIDTH = 10;
        /// <summary>ゲーム盤高さ</summary>
        public const short BOARD_HEIGHT = 21;

        /// <summary>横幅番兵数</summary>
        public const int SENTINEL_WIDTH = 2;
        /// <summary>高さ番兵数</summary>
        public const int SENTINEL_HEIGHT = 5;

        /// <summary>番兵込の盤上の幅</summary>
        public const int WIDTH_WITH_SENTINEL = BOARD_WIDTH + SENTINEL_WIDTH;
        /// <summary>番兵込の盤上の高さ</summary>
        public const int HEIGHT_WITH_SENTINEL = BOARD_HEIGHT + SENTINEL_HEIGHT;

        /// <summary>番兵込の配列サイズ</summary>
        public const int BOARD_ARRAY_SIZE = WIDTH_WITH_SENTINEL * HEIGHT_WITH_SENTINEL;

        /// <summary>相対的に配置されるブロック数</summary>
        public const short RELATIVE_BLOCK_NUM = 3;

        /// <summary>Maxで一度に消える行数</summary>
        public const short MAX_DELETE_LINE_NUM = 4;

        /// <summary>fade時間</summary>
        public const float FADE_DURATION = 0.25f;

        /// <summary>行消えるときの待ち時間</summary>
        public const float WAIT_DELETE_LINE_DURATION = FADE_DURATION + 0.1f;

        /// <summary>ブロックタイプ数</summary>
        public static short BLOCK_TYPE_NUM { get { return (short)blockSettingData.Count; } }

        /// <summary>空ブロック</summary>
        public const short BLOCK_TYPE_NULL = 0;

        /// <summary>配置予定ブロックのSetting Index</summary>
        public const short BLOCK_TYPE_ATTEMPT = 8;

        /// <summary>一本棒(Line)</summary>
        public const short BLOCK_TYPE_LINE = 1;

        /// <summary>次に落ちてくるブロックの表示数</summary>
        public const short SHOW_NEXT_BLOCK_NUM = 2;

        /// <summary>ブロック設定データ(読み取り専用)</summary>
        static public IReadOnlyList<BlockSettingData> BLOCK_DATA
        {
            get { return blockSettingData.AsReadOnly(); }
        }

        /// <summary>ブロック設定データ</summary>
        static readonly private List<BlockSettingData> blockSettingData = new List<BlockSettingData>
        {
            new BlockSettingData(0, new GridPos[]{ new GridPos(0,  0), new GridPos( 0,  0), new GridPos( 0,  0) }), // null

            new BlockSettingData(2, new GridPos[]{ new GridPos(-1, 0), new GridPos( 1,  0), new GridPos( 2,  0) }), // Line
            new BlockSettingData(4, new GridPos[]{ new GridPos(-1, 0), new GridPos(-1,  1), new GridPos( 1,  0) }), // L1
            new BlockSettingData(4, new GridPos[]{ new GridPos(-1, 0), new GridPos( 1,  0), new GridPos( 1,  1) }), // L2
            new BlockSettingData(2, new GridPos[]{ new GridPos(-1, 0), new GridPos( 0,  1), new GridPos( 1,  1) }), // Key1
            new BlockSettingData(2, new GridPos[]{ new GridPos(-1, 1), new GridPos( 0,  1), new GridPos( 1,  0) }), // Key2
            new BlockSettingData(1, new GridPos[]{ new GridPos(0,  1), new GridPos( 1,  0), new GridPos( 1,  1) }), // Square
            new BlockSettingData(4, new GridPos[]{ new GridPos(-1, 0), new GridPos( 0,  1), new GridPos( 1,  0) }), // T
        };
    }
}


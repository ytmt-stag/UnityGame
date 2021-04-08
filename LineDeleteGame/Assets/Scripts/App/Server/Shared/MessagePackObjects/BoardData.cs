using App.Shared.Common;
using MessagePack;

namespace App.Shared.MessagePackObjects
{
    /// <summary>
    /// グリッド位置情報
    /// </summary>
    [MessagePackObject]
    public struct GridPos
    {
        /// <summary>x</summary>
        [Key(0)]
        public short x;
        /// <summary>y</summary>
        [Key(1)]
        public short y;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public GridPos(short x, short y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// ブロック状態
    /// </summary>
    [MessagePackObject(true)]
    public struct BlockStatus
    {
        /// <summary>ブロック位置</summary>
        [Key(0)]
        public GridPos Pos;

        /// <summary>ブロックタイプ</summary>
        [Key(1)]
        public short Type;

        /// <summary>ブロックの回転数</summary>
        [Key(2)]
        public short RotateNum;

        /// <summary>ブロック設定データ</summary>
        [Key(3)]
        public short SettingDataIdx;

        /// <summary>「Line」ブロックか</summary>
        public bool IsLineBlock { get { return Type == SharedConstant.BLOCK_TYPE_LINE; } }
    }
}
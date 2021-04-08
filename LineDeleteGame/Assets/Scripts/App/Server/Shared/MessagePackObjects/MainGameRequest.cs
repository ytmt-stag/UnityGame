using App.Shared.Common;
using MessagePack;
using System.Collections.Generic;

namespace App.Shared.MessagePackObjects
{
    /// <summary>
    /// マッチング時に与えられたルーム名をもとに準備完了
    /// </summary>
    [MessagePackObject]
    public class GameReadyRequest
    {
        [Key(0)]
        public string MineId;

        [Key(1)]
        public string EnemyId;

        [Key(2)]
        public string RoomName;

        [Key(3)]
        public bool WithOther;
    }

    /// <summary>
    /// マッチング時に与えられたルーム名をもとにゲーム開始を宣言
    /// </summary>
    [MessagePackObject]
    public class GameStartRequest
    {
        [Key(0)]
        public string Id;

        [Key(1)]
        public string RoomName;
    }

    /// <summary>
    /// キーコマンド
    /// </summary>
    [MessagePackObject]
    public class GameInputRequest
    {
        [Key(0)]
        public string Id;

        [Key(1)]
        public ePlayCommand Command;
    }

    /// <summary>
    /// 自分のIDを投げてマッチング時に参考にできるようにする想定でRequest作っておく
    /// </summary>
    [MessagePackObject(true)]
    public class GameReadyResponse
    {
        [Key(0)]
        public string RoomName;

        [Key(1)]
        public Dictionary<string, IReadOnlyList<short>> PlayerBoards;
    }

    /// <summary>
    /// 自分のIDを投げてマッチング時に参考にできるようにする想定でRequest作っておく
    /// </summary>
    [MessagePackObject(true)]
    public class GameStartResponse
    {
        [Key(0)]
        public string RoomName;

        [Key(1)]
        public Dictionary<string, IReadOnlyList<short>> PlayerBoards;
    }

    /// <summary>
    /// ゲーム実行中の情報
    /// </summary>
    [MessagePackObject(true)]
    public class ReceivedGameProcessBoardInfo
    {
        /// <summary>盤面情報(番兵込)</summary>
        [Key(0)]
        public IReadOnlyList<short> Board;

        /// <summary>得点</summary>
        [Key(1)]
        public int Score;

        /// <summary>次落ちてくるブロック</summary>
        [Key(2)]
        public List<BlockStatus> BlockStatuses;

        /// <summary>行数削除フェーズか</summary>
        [Key(3)]
        public bool IsDeleteLine;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ReceivedGameProcessBoardInfo()
        {
            BlockStatuses = new List<BlockStatus>(SharedConstant.SHOW_NEXT_BLOCK_NUM);
        }
    }

    /// <summary>
    /// 現在進行中のゲームのプロセス
    /// </summary>
    [MessagePackObject(true)]
    public class GameProcessResponse
    {
        /// <summary>各プレイヤーの情報</summary>
        [Key(0)]
        public Dictionary<string, ReceivedGameProcessBoardInfo> PlayersInfo;
    }
}
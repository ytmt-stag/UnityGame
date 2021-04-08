using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessagePack;

namespace App.Shared.MessagePackObjects
{
    /// <summary>
    /// 自分のIDを投げてマッチング時に参考にできるようにする想定でRequest作っておく
    /// </summary>
    [MessagePackObject]
    public class MatchingJoinRequest
    {
        [Key(0)]
        public bool withOther;
    }

    /// <summary>
    /// マッチングが成功した時
    /// </summary>
    [MessagePackObject(true)]
    public class MatchingRequestSuccessResponse
    {
        [Key(0)]
        public string MineId;

        [Key(1)]
        public string EnemeyId  = "";

        [Key(2)]
        public string RoomName;
    }
}
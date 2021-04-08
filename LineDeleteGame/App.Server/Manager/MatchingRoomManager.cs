using MagicOnion.Server;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace App.Server
{
    /// <summary>
    /// 部屋を管理するシングルトン
    /// </summary>
    public class MatchingRoomManager
    {
        public class Data
        {
            public string Id;
            public Guid Guid;
            public ServiceContext Context;

            public Data(Guid id, ServiceContext context)
            {
                Id = id.ToString();
                Guid = id;
                Context = context;
            }
        }

        /// <summary>マッチング待機中のユーザー</summary>
        private readonly Dictionary<string, Data> waitingUserDict = new Dictionary<string, Data>();

        /// <summary>スレッド lockオブジェクト</summary>
        readonly object gate = new object();

        /// <summary>
        /// コンストラクタDI
        /// </summary>
        /// <param name="config"></param>
        public MatchingRoomManager(IConfiguration config)
        {
        }

        /// <summary>
        /// Room生成
        /// </summary>
        /// <returns></returns>
        public string GenerateRoomId()
        {
            // 本当は一意のRoom名になるように / 今は適当に乱数
            var rand = new Random();
            return rand.Next().ToString();
        }

        /// <summary>
        /// マッチング試行
        /// </summary>
        /// <param name="registerData"></param>
        /// <returns></returns>
        public (string, Data) TryMatching(Data registerData)
        {
            lock (gate)
            {
                if (waitingUserDict.Count >= 1)
                {
                    var roomId = GenerateRoomId();
                    foreach (var dat in waitingUserDict)
                    {
                        return (roomId, dat.Value);
                    }
                }
                else
                {
                    waitingUserDict.TryAdd(registerData.Id, registerData);
                }
            }
            return (string.Empty, null);
        }

        /// <summary>
        /// マッチング部屋から退室
        /// </summary>
        /// <param name="id"></param>
        public void RemoveFromMatchingRoom(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            lock (gate)
            {
                waitingUserDict.Remove(id);
            }
        }
    }
}

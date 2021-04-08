using MessagePack;
using UnityEngine;

namespace App.Shared.MessagePackObjects
{
    [MessagePackObject]
    public class TestPlayer
    {
        [Key(0)]
        public string MyName { get; set; }
        [Key(1)]
        public Vector3 Pos { get; set; }
        [Key(2)]
        public Quaternion Rot { get; set; }
    }
}
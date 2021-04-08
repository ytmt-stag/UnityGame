using System.Collections.Generic;

namespace App
{
    public static class LoopFunction
    {
        private static readonly Dictionary<eLoop, ParamLoopResource> PARAM_LOOP_RESOURCE_DICT = new Dictionary<eLoop, ParamLoopResource>()
        {
            { eLoop.None,                   new ParamLoopResource("") },
            { eLoop.Title,                  new ParamLoopResource("Prefabs/Loop/Title") },
            { eLoop.MainGameAlone,          new ParamLoopResource("Prefabs/Loop/MainGameAlone") },
            { eLoop.MainGameWithNetworking, new ParamLoopResource("Prefabs/Loop/MainGameWithNetworking") },
            { eLoop.Matching,               new ParamLoopResource("Prefabs/Loop/Matching") },
        };

        /// <summary>
        /// LoopのPrefabパス取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ParamLoopResource GetParamLoopResource(eLoop loop)
        {
            return PARAM_LOOP_RESOURCE_DICT[loop];
        }
    }
}


using App.Shared.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace App
{
    /// <summary>
    /// エフェクト更新できるように操作はここで一元管理
    /// </summary>
    public class LineDeleteEffector : MonoBehaviour
    {
        /// <summary>パーティクル予め確保 / 最大エフェクト数は一本線のブロックがMAXなので計算可能</summary>
        private List<ParticleSystem> particlePool = new List<ParticleSystem>(SharedConstant.MAX_DELETE_LINE_NUM * SharedConstant.BOARD_WIDTH);

        /// <summary>盤面タイル情報</summary>
        private BoardRenderer.TileInfo bufTileInfo = null;

        /// <summary>現在の経過秒数</summary>
        private float currentDuration = 0f;

        /// <summary>
        /// 初期化
        /// </summary>
        public void LoadObject(BoardRenderer.TileInfo info)
        {
            bufTileInfo = info;

            // エフェクトプール確保
            var loadedObj = Resources.Load("Prefabs/Effects/BlockDelete/DeleteEffect");
            for (int i = 0; i < particlePool.Capacity; ++i)
            {
                GameObject gameobj = Instantiate(loadedObj) as GameObject;
                ParticleSystem particle = gameobj.GetComponent<ParticleSystem>();
                particle.gameObject.SetActive(false);
                particlePool.Add(particle);

                // 所在なさげなのでこのオブジェクトを親とす
                particle.transform.SetParent(transform, false);
            }
        }

        /// <summary>
        /// 開始
        /// </summary>
        /// <param name="effectIdx"></param>
        /// <param name="pos"></param>
        public void StartEffect(IReadOnlyList<short> willDeleteLineY, bool isResume = false)
        {
            if(!isResume)
            {   // 初回スタートはfalse指定 / Resume/Pauseに対応できるように
                currentDuration = 0;
            }

            int counter = 0;
            foreach (short y in willDeleteLineY)
            {
                for (int x = 0; x < bufTileInfo.Size.x; x++)
                {
                    Vector3Int tilePos = new Vector3Int(bufTileInfo.Origin.x + x, bufTileInfo.Origin.y + y - 1, 0);
                    var pos = bufTileInfo.TileMap.CellToWorld(tilePos);

                    ParticleSystem curParticle = particlePool[counter];
                    curParticle.gameObject.SetActive(true);
                    curParticle.transform.position = pos;
                    curParticle.Play();
                    counter++;
                }
            }
        }

        /// <summary>
        /// エフェクト更新
        /// </summary>
        /// <param name="willDeleteLineY"></param>
        /// <param name="dt"></param>
        public float UpdateEffect(IReadOnlyList<short> willDeleteLineY, float dt, float fadeDuration)
        {
            currentDuration += dt;
            float ratio = currentDuration / fadeDuration;
            float currentAlpha = Mathf.Lerp(1f, 0f, ratio);

            foreach (short y in willDeleteLineY)
            {
                for (int x = 0; x < bufTileInfo.Size.x; x++)
                {
                    Vector3Int tilePos = new Vector3Int(bufTileInfo.Origin.x + x, bufTileInfo.Origin.y + y - 1, 0);
                    bufTileInfo.TileMap.SetTileFlags(tilePos, TileFlags.None);
                    bufTileInfo.TileMap.SetColor(tilePos, new Color(1, 1, 1, currentAlpha));
                    bufTileInfo.TileMap.CellToWorld(tilePos);
                }
            }

            return ratio;
        }

        /// <summary>
        /// 一時停止
        /// </summary>
        /// <param name="effectIdx"></param>
        public void PauseEffect(int effectIdx)
        {
            ParticleSystem curParticle = particlePool[effectIdx];
            curParticle.Pause(true);
        }
    }
}
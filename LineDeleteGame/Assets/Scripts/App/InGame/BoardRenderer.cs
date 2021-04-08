using App.Shared.Common;
using App.Shared.MessagePackObjects;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace App
{
    /// <summary>
    /// ゲーム盤を描画
    /// </summary>
    public class BoardRenderer : MonoBehaviour
    {
        public class TileInfo
        {
            /// <summary>Unity Tilemap</summary>
            public Tilemap TileMap { get; private set; }

            /// <summary>グリッドidx位置</summary>
            public Vector3Int[] GridPosAry { get; private set; }

            /// <summary>Tilemap上の各マスに配置すべきPrefabの参照を保持</summary>
            public TileBase[] TileTypeAry { get; private set; }

            /// <summary>Unity上でのTileの開始位置</summary>
            public Vector3Int Origin { get { return TileMap.origin; } }

            /// <summary>Unity上でのTileサイズ</summary>
            public Vector3Int Size { get { return TileMap.size; } }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="tilemap"></param>
            /// <param name="blankTile"></param>
            public TileInfo(Tilemap tilemap, TileBase blankTile)
            {
                TileMap = tilemap;

                int arySize = Size.x * Size.y;
                GridPosAry = new Vector3Int[arySize];
                TileTypeAry = new TileBase[arySize];
                InitializeGrid(blankTile);
            }

            /// <summary>
            /// Grid初期化
            /// </summary>
            /// <param name="initTile"></param>
            public void InitializeGrid(TileBase initTile)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    for (int x = 0; x < Size.x; x++)
                    {
                        int idx = CalcIdx(x, y, Size.x);
                        Vector3Int tilePos = new Vector3Int(Origin.x + x, Origin.y + y, 0);
                        GridPosAry[idx] = ConvertTileToGrid(tilePos, TileMap);
                        TileTypeAry[idx] = initTile;
                    }
                }
            }

            /// <summary>
            /// タイル描画更新
            /// </summary>
            public void DrawTile()
            {   // これでもまだGCが出て困っている。
                TileMap.SetTilesBlock(TileMap.cellBounds, TileTypeAry);
            }
        }

        /// <summary>ゲーム本体</summary>
        [SerializeField]
        private Tilemap boardTilemap = null;

        /// <summary>次に描画するブロック</summary>
        [SerializeField]
        private Tilemap[] nextTilemap = null;

        /// <summary>各タイルのPrefab</summary>
        [SerializeField]
        private TileBase[] eachTilePrefabs = null;

        /// <summary>空のタイル</summary>
        [SerializeField]
        private TileBase blankTilePrefab = null;

        /// <summary>エフェクト処理</summary>
        [SerializeField]
        private LineDeleteEffector effector = null;

        /// <summary>エフェクト処理</summary>
        public LineDeleteEffector Effector { get { return effector; } }

        /// <summary>ゲーム本体</summary>
        private TileInfo gameTile = null;

        /// <summary>次に落ちてくるブロック</summary>
        private TileInfo nextFallBlockTile = null;

        /// <summary>次の次に落ちてくるブロック</summary>
        private TileInfo afterNextFallBlockTile = null;

        /// <summary>
        /// Unity座標上のTile位置を[0～width, 0～height]のグリッド位置に変換
        /// </summary>
        /// <param name="tilePos"></param>
        /// <returns></returns>
        public static Vector3Int ConvertTileToGrid(Vector3Int tilePos, Tilemap tilemap)
        {
            return tilePos - tilemap.origin;
        }

        /// <summary>
        /// 配列Index計算
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static int CalcIdx(int x, int y, int width)
        {
            return y * width + x;
        }

        /// <summary>
        /// MonoBehaviour Awake
        /// </summary>
        private void Awake()
        {
            // ゲーム盤の配列初期化
            gameTile = new TileInfo(boardTilemap, eachTilePrefabs[0]);

            // 次のブロック領域の初期化
            nextFallBlockTile = new TileInfo(nextTilemap[0], blankTilePrefab);
            afterNextFallBlockTile = new TileInfo(nextTilemap[1], blankTilePrefab);

            // エフェクトプール確保
            effector.LoadObject(gameTile);
        }

        /// <summary>
        /// BoardInfo情報をもとにTile情報更新
        /// </summary>
        /// <param name="board"></param>
        public void UpdateTilePanel(IReadOnlyList<short> board)
        {
            // HasSyncTileCallbackのGCを極力抑えるため、tileTypeAryに格納して一括適用
            for (int y = 0; y < gameTile.Size.y; y++)
            {
                for (int x = 0; x < gameTile.Size.x; x++)
                {
                    int idx = CalcIdx(x, y, gameTile.Size.x);
                    int tileType = BoardInfo.GetBoardTypeFromOriginZero(gameTile.GridPosAry[idx].x, gameTile.GridPosAry[idx].y, board);
                    gameTile.TileTypeAry[idx] = eachTilePrefabs[tileType];

                    // alpha弄ってる可能性があるので戻す
                    Vector3Int tilePos = new Vector3Int(gameTile.Origin.x + x, gameTile.Origin.y + y, 0);
                    boardTilemap.SetTileFlags(tilePos, TileFlags.None);
                    boardTilemap.SetColor(tilePos, new Color(1, 1, 1, 1));
                }
            }

            // これでもまだGCが出て困っている。
            gameTile.DrawTile();
        }

        /// <summary>
        /// 次に落ちてくるブロック更新
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        public void UpdateNextTile(BlockStatus first, BlockStatus second)
        {
            updateNextTile(nextFallBlockTile, first);
            updateNextTile(afterNextFallBlockTile, second);
        }

        /// <summary>
        /// ゲームオーバー描画
        /// </summary>
        public void DrawGameOver(IReadOnlyList<short> board)
        {
            for (int y = 0; y < gameTile.Size.y; y++)
            {
                for (int x = 0; x < gameTile.Size.x; x++)
                {
                    Vector3Int tilePos = new Vector3Int(gameTile.Origin.x + x, gameTile.Origin.y + y, 0);
                    Vector3Int gridPos = ConvertTileToGrid(tilePos, boardTilemap);

                    int tileType = BoardInfo.GetBoardTypeFromOriginZero(gridPos.x, gridPos.y, board) != 0 ? 1 : 0;
                    boardTilemap.SetTile(tilePos, eachTilePrefabs[tileType]);
                }
            }
        }

        /// <summary>
        /// 各「次に落ちてくるブロック」更新
        /// </summary>
        /// <param name="tileInfo"></param>
        /// <param name="target"></param>
        private void updateNextTile(TileInfo tileInfo, BlockStatus target)
        {
            tileInfo.InitializeGrid(blankTilePrefab);
            Vector3Int center = target.IsLineBlock ? new Vector3Int(1, 1, 0) : new Vector3Int(1, 0, 0);

            // (0,0)座標のブロック更新
            int statIdx = CalcIdx(center.x, center.y, tileInfo.Size.x);
            tileInfo.TileTypeAry[statIdx] = eachTilePrefabs[target.Type];

            // 相対座標分のブロック更新
            for (int i = 0; i < SharedConstant.RELATIVE_BLOCK_NUM; i++)
            {
                BlockSettingData settingData = SharedConstant.BLOCK_DATA[target.SettingDataIdx];
                GridPos diffPos = BoardInfo.CalcRelativeRotatePos(settingData.RelativePos[i], settingData.RotatableNum, target.RotateNum);

                int expectedIdx = CalcIdx(center.x + diffPos.x, center.y + diffPos.y, tileInfo.Size.x);
                tileInfo.TileTypeAry[expectedIdx] = eachTilePrefabs[target.Type];
            }

            tileInfo.DrawTile();
        }
    }
}

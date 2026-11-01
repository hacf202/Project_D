using System;
using System.Collections.Generic;
using UnityEngine;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Interactions;

namespace Project_D.Dungeon.Room
{
    public class RoomLayout : MonoBehaviour
    {
        [Header("Room Identity")]
        [SerializeField] private string _roomName = "Arena Layout";
        [SerializeField] private RunNodeType _supportedType = RunNodeType.NormalBattle;
        [SerializeField] private Vector2 _roomSize = new Vector2(30f, 30f);

        [Header("Spawn Points")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Transform[] _enemySpawnPoints;

        [Header("Portals (Cổng Dịch Chuyển)")]
        [SerializeField] private RoomPortal[] _portals;

        [Header("Interaction Points (Điểm Tương Tác Phi Chiến Đấu)")]
        [SerializeField] private InteractionPoint[] _interactionPoints;

        [Header("Obstacles & Boundaries")]
        [SerializeField] private Transform _obstaclesRoot;
        [SerializeField] private Transform _floorRoot;

        public string RoomName => _roomName;
        public RunNodeType SupportedType => _supportedType;
        public Vector2 RoomSize => _roomSize;
        public Transform PlayerSpawnPoint => _playerSpawnPoint != null ? _playerSpawnPoint : transform;
        public Transform[] EnemySpawnPoints => _enemySpawnPoints;
        public RoomPortal[] Portals => _portals;
        public InteractionPoint[] InteractionPoints => _interactionPoints;
        public Transform ObstaclesRoot => _obstaclesRoot;
        public Transform FloorRoot => _floorRoot;

        public void SetIdentity(string roomName, RunNodeType type, Vector2 size)
        {
            _roomName = roomName;
            _supportedType = type;
            _roomSize = size;
        }

        private void Awake()
        {
            AutoBindReferences();
        }

#if UNITY_EDITOR
        [ContextMenu("Bake Tilemap Entities to GameObjects")]
        public void BakeTilemapEntitiesToPermanentObjects()
        {
            // 1. Quét tìm tất cả các clone hoặc đối tượng entities hiện có
            var allChildren = GetComponentsInChildren<Transform>(true);
            var targets = new List<Transform>();
            foreach (var t in allChildren)
            {
                if (t != null && t != transform)
                {
                    if (t.name.IndexOf("PlayerSpawn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        t.name.IndexOf("EnemySpawn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        t.GetComponent<RoomPortal>() != null || t.name.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        t.GetComponent<InteractionPoint>() != null)
                    {
                        targets.Add(t);
                    }
                }
            }

            if (targets.Count == 0) return;

            // 2. Chuẩn bị thư mục SpawnPoints chuẩn
            Transform spawnPointsRoot = transform.Find("SpawnPoints");
            if (spawnPointsRoot == null)
            {
                var spGo = new GameObject("SpawnPoints");
                spGo.transform.SetParent(transform, false);
                spawnPointsRoot = spGo.transform;
            }

            Transform enemySpawnsRoot = spawnPointsRoot.Find("EnemySpawns");
            if (enemySpawnsRoot == null)
            {
                var esGo = new GameObject("EnemySpawns");
                esGo.transform.SetParent(spawnPointsRoot, false);
                enemySpawnsRoot = esGo.transform;
            }

            Transform portalsRoot = spawnPointsRoot.Find("Portals");
            if (portalsRoot == null)
            {
                var pGo = new GameObject("Portals");
                pGo.transform.SetParent(spawnPointsRoot, false);
                portalsRoot = pGo.transform;
            }

            Transform interactionsRoot = spawnPointsRoot.Find("Interactions");
            if (interactionsRoot == null)
            {
                var iGo = new GameObject("Interactions");
                iGo.transform.SetParent(spawnPointsRoot, false);
                interactionsRoot = iGo.transform;
            }

            // 3. Chuyển đổi từng đối tượng thành GameObject thật sự độc lập (không bị Tilemap quản lý)
            foreach (var tr in targets)
            {
                if (tr == null || tr == transform || tr == spawnPointsRoot || tr == enemySpawnsRoot || tr == portalsRoot || tr == interactionsRoot) continue;

                // Nếu đối tượng là Clone hoặc con của Tilemap thì cần bake thành GameObject vĩnh viễn
                if (tr.name.IndexOf("(Clone)", StringComparison.OrdinalIgnoreCase) >= 0 || (tr.parent != null && tr.parent.name.IndexOf("Tilemap", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Vector3 worldPos = tr.position;
                    Quaternion worldRot = tr.rotation;
                    string cleanName = tr.name.Replace("(Clone)", "").Trim();

                    // Tìm Prefab gốc từ asset
                    GameObject sourcePrefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(tr.gameObject);
                    if (sourcePrefab == null)
                    {
                        string pPath = $"Assets/_Project/Prefabs/Dungeon/LevelDesign/{cleanName}.prefab";
                        sourcePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                    }

                    GameObject realGo = null;
                    if (sourcePrefab != null)
                    {
                        realGo = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(sourcePrefab);
                    }
                    else
                    {
                        realGo = Instantiate(tr.gameObject);
                    }

                    realGo.name = cleanName;
                    realGo.transform.position = worldPos;
                    realGo.transform.rotation = worldRot;
                    realGo.hideFlags = HideFlags.None;

                    // Phân loại vào thư mục con phù hợp
                    if (cleanName.IndexOf("PlayerSpawn", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        realGo.transform.SetParent(spawnPointsRoot, true);
                    }
                    else if (cleanName.IndexOf("EnemySpawn", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        realGo.transform.SetParent(enemySpawnsRoot, true);
                    }
                    else if (realGo.GetComponent<RoomPortal>() != null || cleanName.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        realGo.transform.SetParent(portalsRoot, true);
                    }
                    else if (realGo.GetComponent<InteractionPoint>() != null)
                    {
                        realGo.transform.SetParent(interactionsRoot, true);
                    }
                    else
                    {
                        realGo.transform.SetParent(spawnPointsRoot, true);
                    }

                    // Hủy đối tượng clone tạm thời do Tilemap tạo ra
                    DestroyImmediate(tr.gameObject);
                }
            }

            // 4. Xóa hoàn toàn Tilemap_Entities vì toàn bộ quái/cổng đã được bake thành GameObject thật
            var entitiesGo = transform.Find("Tilemap_Entities");
            if (entitiesGo != null)
            {
                var entTm = entitiesGo.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                if (entTm != null) entTm.ClearAllTiles();
                DestroyImmediate(entitiesGo.gameObject);
            }

            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif

        [ContextMenu("Auto Bind Child References")]
        public void AutoBindReferences()
        {
#if UNITY_EDITOR
            if (transform.Find("Tilemap_Entities") != null)
            {
                BakeTilemapEntitiesToPermanentObjects();
            }
#endif
            // 1. Tìm PlayerSpawnPoint
            if (_playerSpawnPoint == null)
            {
                var spawn = transform.Find("SpawnPoints/PlayerSpawnPoint") ?? transform.Find("PlayerSpawnPoint");
                if (spawn != null)
                {
                    _playerSpawnPoint = spawn;
                }
                else
                {
                    foreach (Transform t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t != transform && t.name.IndexOf("PlayerSpawn", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _playerSpawnPoint = t;
                            break;
                        }
                    }
                }
            }

            // 2. Tìm EnemySpawnPoints
            var list = new List<Transform>();
            var enemySpawnsRoot = transform.Find("SpawnPoints/EnemySpawns") ?? transform.Find("EnemySpawns");
            if (enemySpawnsRoot != null)
            {
                foreach (Transform child in enemySpawnsRoot)
                {
                    if (child != null) list.Add(child);
                }
            }

            if (list.Count == 0)
            {
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                {
                    if (t != transform && t != _playerSpawnPoint && t != enemySpawnsRoot && t.name.IndexOf("EnemySpawnPoint", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        list.Add(t);
                    }
                }
            }
            if (list.Count > 0)
            {
                _enemySpawnPoints = list.ToArray();
            }

            // 3. Tìm Portals
            _portals = GetComponentsInChildren<RoomPortal>(true);

            // Sắp xếp các cổng theo thứ tự từ trái sang phải theo trục X
            if (_portals != null && _portals.Length > 1)
            {
                Array.Sort(_portals, (a, b) =>
                {
                    if (a == null || b == null) return 0;
                    return a.transform.localPosition.x.CompareTo(b.transform.localPosition.x);
                });
            }

            // 4. Tìm Điểm Tương Tác
            _interactionPoints = GetComponentsInChildren<InteractionPoint>(true);

            // 5. Thư mục gốc Obstacles & Floor
            if (_obstaclesRoot == null)
            {
                _obstaclesRoot = transform.Find("Obstacles") ?? transform.Find("Tilemap_Walls");
            }

            if (_floorRoot == null)
            {
                _floorRoot = transform.Find("Floor") ?? transform.Find("Tilemap_Floor");
            }

            CalculateBoundsFromChildren();
        }

        // Tự động tính toán kích thước bao quanh phòng dựa trên toàn bộ Collider và Tilemap
        public Vector2 CalculateBoundsFromChildren()
        {
            bool foundValid = false;
            Bounds bounds = default;

            // 1. Quét toàn bộ Collider2D (bao gồm BoxCollider2D, CircleCollider2D, TilemapCollider2D)
            var colliders = GetComponentsInChildren<Collider2D>();
            if (colliders != null && colliders.Length > 0)
            {
                foreach (var col in colliders)
                {
                    if (col.isTrigger) continue;

                    if (!foundValid)
                    {
                        bounds = col.bounds;
                        foundValid = true;
                    }
                    else
                    {
                        bounds.Encapsulate(col.bounds);
                    }
                }
            }

            // 2. Quét thêm TilemapRenderer (để bao quát cả diện tích sàn Tilemap nếu sàn không có Collider)
            var tmRenderers = GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>();
            if (tmRenderers != null && tmRenderers.Length > 0)
            {
                foreach (var tr in tmRenderers)
                {
                    var tm = tr.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                    if (tm != null && tm.size.x > 0 && tm.size.y > 0)
                    {
                        if (!foundValid)
                        {
                            bounds = tr.bounds;
                            foundValid = true;
                        }
                        else
                        {
                            bounds.Encapsulate(tr.bounds);
                        }
                    }
                }
            }

            if (foundValid)
            {
                _roomSize = new Vector2(Mathf.Ceil(bounds.size.x), Mathf.Ceil(bounds.size.y));
            }
            return _roomSize;
        }

        // Vẽ Gizmos trực quan trong cửa sổ Scene View để Level Designer dễ quan sát
        private void OnDrawGizmos()
        {
            DrawRoomGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawRoomGizmos(true);
        }

        private void DrawRoomGizmos(bool selected)
        {
            float alpha = selected ? 0.9f : 0.4f;

            // 1. Viền bao quanh kích thước phòng
            Gizmos.color = selected ? new Color(0f, 0.8f, 1f, 0.8f) : new Color(0.7f, 0.7f, 0.7f, 0.3f);
            Gizmos.DrawWireCube(transform.position, new Vector3(_roomSize.x, _roomSize.y, 0f));

            // 2. Điểm xuất hiện của Player (Màu xanh lá)
            if (_playerSpawnPoint != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, alpha);
                Gizmos.DrawWireSphere(_playerSpawnPoint.position, 0.8f);
                Gizmos.DrawLine(_playerSpawnPoint.position + Vector3.left * 1.2f, _playerSpawnPoint.position + Vector3.right * 1.2f);
                Gizmos.DrawLine(_playerSpawnPoint.position + Vector3.down * 1.2f, _playerSpawnPoint.position + Vector3.up * 1.2f);
#if UNITY_EDITOR
                if (selected)
                {
                    UnityEditor.Handles.Label(_playerSpawnPoint.position + Vector3.up * 0.9f, "[Player Spawn]");
                }
#endif
            }

            // 3. Các điểm xuất hiện quái vật chiến thuật (Màu đỏ)
            if (_enemySpawnPoints != null)
            {
                for (int i = 0; i < _enemySpawnPoints.Length; i++)
                {
                    var pt = _enemySpawnPoints[i];
                    if (pt != null)
                    {
                        Gizmos.color = new Color(1f, 0.25f, 0.25f, alpha);
                        Gizmos.DrawWireSphere(pt.position, 0.6f);
                        Gizmos.DrawSphere(pt.position, 0.2f);
#if UNITY_EDITOR
                        if (selected)
                        {
                            UnityEditor.Handles.Label(pt.position + Vector3.up * 0.7f, $"Spawn #{i + 1}");
                        }
#endif
                    }
                }
            }

            // 4. Các cổng dịch chuyển (Màu cyan)
            if (_portals != null)
            {
                for (int i = 0; i < _portals.Length; i++)
                {
                    var portal = _portals[i];
                    if (portal != null)
                    {
                        Gizmos.color = new Color(0.2f, 0.9f, 1f, alpha);
                        Gizmos.DrawWireSphere(portal.transform.position, 1.2f);
#if UNITY_EDITOR
                        if (selected)
                        {
                            UnityEditor.Handles.Label(portal.transform.position + Vector3.down * 1.4f, $"Cổng #{i + 1}");
                        }
#endif
                    }
                }
            }

            // 5. Điểm tương tác (Màu vàng kim)
            if (_interactionPoints != null)
            {
                foreach (var point in _interactionPoints)
                {
                    if (point != null)
                    {
                        Gizmos.color = new Color(1f, 0.85f, 0.2f, alpha);
                        Gizmos.DrawWireCube(point.transform.position, Vector3.one * 1.5f);
#if UNITY_EDITOR
                        if (selected)
                        {
                            UnityEditor.Handles.Label(point.transform.position + Vector3.up * 1.1f, "[Tương Tác]");
                        }
#endif
                    }
                }
            }
        }
    }
}

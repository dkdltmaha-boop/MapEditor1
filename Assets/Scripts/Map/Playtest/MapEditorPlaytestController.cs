using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorAnimationClock
{
    private static bool playtestActive;
    private static float playtestTime;

    public static float Time => playtestActive ? playtestTime : UnityEngine.Time.realtimeSinceStartup;

    public static void Begin()
    {
        playtestActive = true;
        playtestTime = 0f;
    }

    public static void Advance(float deltaTime)
    {
        if (playtestActive) playtestTime += Mathf.Max(0f, deltaTime);
    }

    public static void End()
    {
        playtestActive = false;
    }
}

[DisallowMultipleComponent]
public sealed class MapEditorPlaytestController : MonoBehaviour
{
    private const string OverlayName = "MapEditor_PlaytestOverlay";
    private const string HudName = "MapEditor_PlaytestHud";
    private const string PlayerTexturePath = "PixelChromaPlayer/Char_Main_Anime";
    private const float PlayerSpeedTilesPerSecond = 4f;
    private const float PlayerHalfSize = 0.3f;
    private const int RegionPixelsPerTile = 16;

    private sealed class MovingRegionPreview
    {
        public MapEditorMovingRegionData data;
        public RectTransform root;
        public Vector2 sourceTopLeft;
        public Vector2 currentTopLeft;
        public int pathIndex;
        public int direction = 1;
        public float waitRemaining;
        public Texture2D texture;
        public Color32[] pixels;
        public Texture2D sourceTexture;
        public Color32[] sourcePixels;
        public float nextTextureRefresh;
    }

    private readonly List<MovingRegionPreview> movingPreviews = new List<MovingRegionPreview>();
    private MapEditorManager manager;
    private GridGenerator generator;
    private RectTransform overlay;
    private RectTransform player;
    private GameObject hud;
    private Text statusText;
    private Button pauseButton;
    private Vector2 playerPosition;
    private string activeRole = "Runner";
    private bool paused;

    public bool IsActive { get; private set; }

    public void StartPlaytest(MapEditorManager owner)
    {
        if (IsActive || owner == null) return;

        manager = owner;
        generator = owner.GridGenerator != null ? owner.GridGenerator : owner.GetComponent<GridGenerator>();
        if (!TryValidate(out string error))
        {
            MapEditorModalPanel.Show(owner, L("맵 테스트를 시작할 수 없습니다", "Cannot Start Map Test"), error, new Color(0.92f, 0.3f, 0.26f, 1f));
            return;
        }

        IsActive = true;
        paused = false;
        MapEditorAnimationClock.Begin();
        CreateMapOverlay();
        CreateHud();
        BuildMovingRegionPreviews();
        ResetPlayer();
        UpdateHud();
        Debug.Log("맵 테스트 시작: 편집 데이터는 변경되지 않습니다.");
    }

    public void StopPlaytest()
    {
        if (!IsActive && overlay == null && hud == null) return;

        IsActive = false;
        MapEditorAnimationClock.End();
        for (int i = 0; i < movingPreviews.Count; i++)
        {
            if (movingPreviews[i].texture != null) Object.Destroy(movingPreviews[i].texture);
            if (movingPreviews[i].sourceTexture != null) Object.Destroy(movingPreviews[i].sourceTexture);
        }
        DestroyRuntimeObject(overlay == null ? null : overlay.gameObject);
        DestroyRuntimeObject(hud);
        overlay = null;
        player = null;
        hud = null;
        statusText = null;
        pauseButton = null;
        movingPreviews.Clear();
        manager?.RefreshAllCells();
        Debug.Log("맵 테스트 종료: 편집 상태로 돌아갑니다.");
    }

    private void OnDisable()
    {
        StopPlaytest();
    }

    private void Update()
    {
        if (!IsActive) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F5))
        {
            StopPlaytest();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
        if (Input.GetKeyDown(KeyCode.R)) ResetPlayer();
        if (paused) return;

        float deltaTime = UnityEngine.Time.unscaledDeltaTime;
        MapEditorAnimationClock.Advance(deltaTime);
        UpdateMovingRegions(deltaTime);
        UpdatePlayer(deltaTime);
    }

    private bool TryValidate(out string error)
    {
        error = string.Empty;
        if (manager.CurrentMapData == null || generator == null || generator.gridParent == null)
        {
            error = L("맵 데이터 또는 그리드를 찾을 수 없습니다.", "Map data or grid is unavailable.");
            return false;
        }

        MapEditorSpawnPointData[] spawns = manager.GetPlaytestSpawnPoints();
        if (spawns == null || spawns.Length == 0)
        {
            error = L("먼저 플레이어 시작 위치를 지정하세요.", "Set a player spawn before testing.");
            return false;
        }

        for (int i = 0; i < spawns.Length; i++)
        {
            MapEditorSpawnPointData spawn = spawns[i];
            if (spawn != null && manager.HasPlaytestGroundAt(spawn.x, spawn.y) && !manager.HasPlaytestCollisionAt(spawn.x, spawn.y)) return true;
        }

        error = L("시작 위치가 바닥 위에 있지 않거나 충돌 벽과 겹칩니다.", "Every spawn is outside walkable ground or overlaps collision.");
        return false;
    }

    private void CreateMapOverlay()
    {
        Transform old = generator.gridParent.Find(OverlayName);
        if (old != null) DestroyRuntimeObject(old.gameObject);

        GameObject overlayObject = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(generator.gridParent, false);
        overlay = overlayObject.GetComponent<RectTransform>();
        overlay.anchorMin = new Vector2(0f, 1f);
        overlay.anchorMax = new Vector2(0f, 1f);
        overlay.pivot = new Vector2(0f, 1f);
        overlay.anchoredPosition = Vector2.zero;
        overlay.sizeDelta = new Vector2(manager.CurrentMapData.width * generator.cellSize, manager.CurrentMapData.height * generator.cellSize);
        Image blocker = overlayObject.GetComponent<Image>();
        blocker.color = Color.clear;
        blocker.raycastTarget = true;
        overlay.SetAsLastSibling();

        GameObject playerObject = new GameObject("Player", typeof(RectTransform), typeof(RawImage));
        playerObject.transform.SetParent(overlay, false);
        player = playerObject.GetComponent<RectTransform>();
        player.anchorMin = new Vector2(0f, 1f);
        player.anchorMax = new Vector2(0f, 1f);
        player.pivot = new Vector2(0.5f, 0.5f);
        player.sizeDelta = Vector2.one * generator.cellSize;

        RawImage image = playerObject.GetComponent<RawImage>();
        Texture2D texture = Resources.Load<Texture2D>(PlayerTexturePath);
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            image.uvRect = new Rect(0f, 45f / texture.height, 16f / texture.width, 16f / texture.height);
        }
    }

    private void CreateHud()
    {
        Canvas canvas = MapEditorSceneUiBuilder.FindEditorCanvas();
        if (canvas == null) return;

        Transform old = canvas.transform.Find(HudName);
        if (old != null) DestroyRuntimeObject(old.gameObject);
        hud = new GameObject(HudName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        hud.transform.SetParent(canvas.transform, false);
        RectTransform rect = hud.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -12f);
        rect.sizeDelta = new Vector2(720f, 42f);
        hud.GetComponent<Image>().color = new Color(0.055f, 0.07f, 0.08f, 0.96f);
        Canvas nestedCanvas = hud.GetComponent<Canvas>();
        nestedCanvas.overrideSorting = true;
        nestedCanvas.sortingOrder = 32000;

        statusText = CreateText(hud.transform, "Status", new Vector2(10f, -6f), new Vector2(245f, 30f), TextAnchor.MiddleLeft, 13);
        CreateButton(hud.transform, L("플레이어", "Runner"), new Vector2(260f, -6f), new Vector2(72f, 30f), () => SetActiveRole("Runner"));
        CreateButton(hud.transform, L("술래", "Seeker"), new Vector2(336f, -6f), new Vector2(72f, 30f), () => SetActiveRole("Seeker"));
        pauseButton = CreateButton(hud.transform, L("정지", "Pause"), new Vector2(412f, -6f), new Vector2(82f, 30f), TogglePause);
        CreateButton(hud.transform, L("재시작", "Restart"), new Vector2(498f, -6f), new Vector2(82f, 30f), ResetPlayer);
        CreateButton(hud.transform, L("테스트 종료", "Exit Test"), new Vector2(584f, -6f), new Vector2(126f, 30f), StopPlaytest);
        hud.transform.SetAsLastSibling();
    }

    private void ResetPlayer()
    {
        MapEditorSpawnPointData[] spawns = manager.GetPlaytestSpawnPoints();
        MapEditorSpawnPointData chosen = FindSpawn(spawns, activeRole) ?? FindFirstWalkableSpawn(spawns);
        if (chosen == null) return;

        activeRole = NormalizeRole(chosen.role);
        playerPosition = new Vector2(chosen.x + 0.5f, chosen.y + 0.5f);
        UpdatePlayerVisual();
        UpdateHud();
    }

    private void UpdatePlayer(float deltaTime)
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();
        if (input.sqrMagnitude <= 0.001f) return;

        Vector2 delta = new Vector2(input.x, -input.y) * PlayerSpeedTilesPerSecond * deltaTime;
        Vector2 next = playerPosition;
        next.x += delta.x;
        if (IsPositionWalkable(next)) playerPosition.x = next.x;

        next = playerPosition;
        next.y += delta.y;
        if (IsPositionWalkable(next)) playerPosition.y = next.y;
        UpdatePlayerVisual();
    }

    private bool IsPositionWalkable(Vector2 position)
    {
        return IsPointWalkable(position + new Vector2(-PlayerHalfSize, -PlayerHalfSize))
            && IsPointWalkable(position + new Vector2(PlayerHalfSize, -PlayerHalfSize))
            && IsPointWalkable(position + new Vector2(-PlayerHalfSize, PlayerHalfSize))
            && IsPointWalkable(position + new Vector2(PlayerHalfSize, PlayerHalfSize));
    }

    private bool IsPointWalkable(Vector2 point)
    {
        int x = Mathf.FloorToInt(point.x);
        int y = Mathf.FloorToInt(point.y);
        bool baseGround = HasStaticGroundAt(x, y);
        bool baseCollision = HasStaticCollisionAt(x, y);

        bool movingGround = false;
        bool movingCollision = false;
        for (int i = 0; i < movingPreviews.Count; i++)
        {
            MovingRegionPreview preview = movingPreviews[i];
            if (!Contains(preview.currentTopLeft, preview.data.width, preview.data.height, point)) continue;
            int sourceX = preview.data.x + Mathf.FloorToInt(point.x - preview.currentTopLeft.x);
            int sourceY = preview.data.y + Mathf.FloorToInt(point.y - preview.currentTopLeft.y);
            movingGround |= manager.HasPlaytestGroundAt(sourceX, sourceY, preview.data.canvasLayerIndex);
            movingCollision |= manager.HasPlaytestCollisionAt(sourceX, sourceY);
        }

        return (baseGround || movingGround) && !(baseCollision || movingCollision);
    }

    private bool HasStaticGroundAt(int x, int y)
    {
        Vector2 cellCenter = new Vector2(x + 0.5f, y + 0.5f);
        for (int canvasIndex = 0; canvasIndex < MapEditorLayerUtility.CanvasLayerCount; canvasIndex++)
        {
            if (!manager.HasPlaytestGroundAt(x, y, canvasIndex)) continue;

            bool movedAway = false;
            for (int i = 0; i < movingPreviews.Count; i++)
            {
                MovingRegionPreview preview = movingPreviews[i];
                if (preview.data.canvasLayerIndex == canvasIndex
                    && Contains(preview.sourceTopLeft, preview.data.width, preview.data.height, cellCenter))
                {
                    movedAway = true;
                    break;
                }
            }

            if (!movedAway) return true;
        }

        return false;
    }

    private bool HasStaticCollisionAt(int x, int y)
    {
        if (!manager.HasPlaytestCollisionAt(x, y)) return false;

        Vector2 cellCenter = new Vector2(x + 0.5f, y + 0.5f);
        for (int i = 0; i < movingPreviews.Count; i++)
        {
            MovingRegionPreview preview = movingPreviews[i];
            if (Contains(preview.sourceTopLeft, preview.data.width, preview.data.height, cellCenter)) return false;
        }

        return true;
    }

    private void UpdatePlayerVisual()
    {
        if (player == null) return;
        player.anchoredPosition = new Vector2(playerPosition.x * generator.cellSize, -playerPosition.y * generator.cellSize);
        player.SetAsLastSibling();
    }

    private void BuildMovingRegionPreviews()
    {
        MapEditorMovingRegionData[] regions = manager.GetPlaytestMovingRegions();
        for (int i = 0; i < regions.Length; i++)
        {
            MapEditorMovingRegionData data = regions[i];
            if (data == null || data.width <= 0 || data.height <= 0 || data.path == null || data.path.Length < 2) continue;

            int textureWidth = data.width * RegionPixelsPerTile;
            int textureHeight = data.height * RegionPixelsPerTile;
            Color32[] pixels = new Color32[textureWidth * textureHeight];
            for (int y = 0; y < data.height; y++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    manager.WriteCanvasCellPixels(
                        data.x + x,
                        data.y + y,
                        data.canvasLayerIndex,
                        RegionPixelsPerTile,
                        pixels,
                        textureWidth,
                        x * RegionPixelsPerTile,
                        (data.height - y - 1) * RegionPixelsPerTile);
                }
            }

            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            Color32[] sourcePixels = new Color32[textureWidth * textureHeight];
            for (int y = 0; y < data.height; y++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    manager.WriteCompositeCellPixelsExcludingCanvas(
                        data.x + x,
                        data.y + y,
                        data.canvasLayerIndex,
                        RegionPixelsPerTile,
                        sourcePixels,
                        textureWidth,
                        x * RegionPixelsPerTile,
                        (data.height - y - 1) * RegionPixelsPerTile);
                }
            }

            Texture2D sourceTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            sourceTexture.SetPixels32(sourcePixels);
            sourceTexture.Apply(false, false);

            GameObject coverObject = new GameObject("MovingRegionSource_" + i, typeof(RectTransform), typeof(RawImage));
            coverObject.transform.SetParent(overlay, false);
            RectTransform coverRect = coverObject.GetComponent<RectTransform>();
            coverRect.anchorMin = new Vector2(0f, 1f);
            coverRect.anchorMax = new Vector2(0f, 1f);
            coverRect.pivot = new Vector2(0f, 1f);
            coverRect.anchoredPosition = new Vector2(data.x * generator.cellSize, -data.y * generator.cellSize);
            coverRect.sizeDelta = new Vector2(data.width * generator.cellSize, data.height * generator.cellSize);
            RawImage cover = coverObject.GetComponent<RawImage>();
            cover.texture = sourceTexture;
            cover.color = Color.white;
            cover.raycastTarget = false;

            GameObject regionObject = new GameObject("MovingRegion_" + i, typeof(RectTransform), typeof(RawImage));
            regionObject.transform.SetParent(overlay, false);
            RectTransform regionRect = regionObject.GetComponent<RectTransform>();
            regionRect.anchorMin = new Vector2(0f, 1f);
            regionRect.anchorMax = new Vector2(0f, 1f);
            regionRect.pivot = new Vector2(0f, 1f);
            regionRect.sizeDelta = new Vector2(data.width * generator.cellSize, data.height * generator.cellSize);
            regionObject.GetComponent<RawImage>().texture = texture;
            regionObject.GetComponent<RawImage>().raycastTarget = false;

            MovingRegionPreview preview = new MovingRegionPreview
            {
                data = data,
                root = regionRect,
                sourceTopLeft = new Vector2(data.x, data.y),
                currentTopLeft = new Vector2(data.x, data.y),
                pathIndex = 1,
                texture = texture,
                pixels = pixels,
                sourceTexture = sourceTexture,
                sourcePixels = sourcePixels
            };
            movingPreviews.Add(preview);
            UpdateMovingRegionVisual(preview);
        }
    }

    private void UpdateMovingRegions(float deltaTime)
    {
        for (int i = 0; i < movingPreviews.Count; i++)
        {
            MovingRegionPreview preview = movingPreviews[i];
            Vector2 previousTopLeft = preview.currentTopLeft;
            bool carryPlayer = IsPlayerStandingOn(preview, previousTopLeft);
            if (MapEditorAnimationClock.Time >= preview.nextTextureRefresh)
            {
                RefreshMovingRegionTexture(preview);
                RefreshMovingRegionSourceTexture(preview);
                preview.nextTextureRefresh = MapEditorAnimationClock.Time + 0.1f;
            }

            if (preview.waitRemaining > 0f)
            {
                preview.waitRemaining -= deltaTime;
                continue;
            }

            MapEditorPathPointData point = preview.data.path[preview.pathIndex];
            Vector2 firstCenter = new Vector2(preview.data.path[0].x, preview.data.path[0].y);
            Vector2 targetTopLeft = preview.sourceTopLeft + new Vector2(point.x, point.y) - firstCenter;
            preview.currentTopLeft = Vector2.MoveTowards(
                preview.currentTopLeft,
                targetTopLeft,
                Mathf.Max(0.05f, preview.data.tilesPerSecond) * deltaTime);
            if (carryPlayer) playerPosition += preview.currentTopLeft - previousTopLeft;
            UpdateMovingRegionVisual(preview);

            if ((preview.currentTopLeft - targetTopLeft).sqrMagnitude > 0.0001f) continue;
            preview.waitRemaining = Mathf.Max(0f, preview.data.waitSeconds);
            AdvanceMovingPath(preview);
        }

        UpdatePlayerVisual();
    }

    private bool IsPlayerStandingOn(MovingRegionPreview preview, Vector2 topLeft)
    {
        if (!Contains(topLeft, preview.data.width, preview.data.height, playerPosition)) return false;
        int sourceX = preview.data.x + Mathf.FloorToInt(playerPosition.x - topLeft.x);
        int sourceY = preview.data.y + Mathf.FloorToInt(playerPosition.y - topLeft.y);
        return manager.HasPlaytestGroundAt(sourceX, sourceY, preview.data.canvasLayerIndex)
            && !manager.HasPlaytestCollisionAt(sourceX, sourceY);
    }

    private static void AdvanceMovingPath(MovingRegionPreview preview)
    {
        int next = preview.pathIndex + preview.direction;
        if (next >= preview.data.path.Length || next < 0)
        {
            if (preview.data.pingPong)
            {
                preview.direction *= -1;
                next = preview.pathIndex + preview.direction;
            }
            else if (preview.data.loop)
            {
                next = 0;
            }
            else
            {
                next = preview.pathIndex;
            }
        }

        preview.pathIndex = Mathf.Clamp(next, 0, preview.data.path.Length - 1);
    }

    private void UpdateMovingRegionVisual(MovingRegionPreview preview)
    {
        preview.root.anchoredPosition = new Vector2(preview.currentTopLeft.x * generator.cellSize, -preview.currentTopLeft.y * generator.cellSize);
    }

    private void RefreshMovingRegionTexture(MovingRegionPreview preview)
    {
        int textureWidth = preview.data.width * RegionPixelsPerTile;
        for (int y = 0; y < preview.data.height; y++)
        {
            for (int x = 0; x < preview.data.width; x++)
            {
                manager.WriteCanvasCellPixels(
                    preview.data.x + x,
                    preview.data.y + y,
                    preview.data.canvasLayerIndex,
                    RegionPixelsPerTile,
                    preview.pixels,
                    textureWidth,
                    x * RegionPixelsPerTile,
                    (preview.data.height - y - 1) * RegionPixelsPerTile);
            }
        }

        preview.texture.SetPixels32(preview.pixels);
        preview.texture.Apply(false, false);
    }

    private void RefreshMovingRegionSourceTexture(MovingRegionPreview preview)
    {
        int textureWidth = preview.data.width * RegionPixelsPerTile;
        for (int y = 0; y < preview.data.height; y++)
        {
            for (int x = 0; x < preview.data.width; x++)
            {
                manager.WriteCompositeCellPixelsExcludingCanvas(
                    preview.data.x + x,
                    preview.data.y + y,
                    preview.data.canvasLayerIndex,
                    RegionPixelsPerTile,
                    preview.sourcePixels,
                    textureWidth,
                    x * RegionPixelsPerTile,
                    (preview.data.height - y - 1) * RegionPixelsPerTile);
            }
        }

        preview.sourceTexture.SetPixels32(preview.sourcePixels);
        preview.sourceTexture.Apply(false, false);
    }

    private void SetActiveRole(string role)
    {
        MapEditorSpawnPointData spawn = FindSpawn(manager.GetPlaytestSpawnPoints(), role);
        if (spawn == null)
        {
            statusText.text = role == "Seeker"
                ? L("술래 시작 위치가 없습니다.", "No seeker spawn is set.")
                : L("플레이어 시작 위치가 없습니다.", "No runner spawn is set.");
            return;
        }

        if (!manager.HasPlaytestGroundAt(spawn.x, spawn.y) || manager.HasPlaytestCollisionAt(spawn.x, spawn.y))
        {
            statusText.text = L("선택한 시작 위치는 이동 가능한 바닥이 아닙니다.", "The selected spawn is not walkable.");
            return;
        }

        activeRole = role;
        ResetPlayer();
    }

    private void TogglePause()
    {
        paused = !paused;
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (statusText != null)
        {
            statusText.text = paused
                ? L("일시정지", "Paused")
                : L("맵 테스트 · " + RoleLabel() + " · WASD/방향키", "Map Test · " + activeRole + " · WASD/Arrows");
        }

        Text pauseLabel = pauseButton == null ? null : pauseButton.GetComponentInChildren<Text>();
        if (pauseLabel != null) pauseLabel.text = paused ? L("계속 [Space]", "Resume [Space]") : L("정지 [Space]", "Pause [Space]");
    }

    private string RoleLabel()
    {
        return activeRole == "Seeker" ? "술래" : "플레이어";
    }

    private static MapEditorSpawnPointData FindSpawn(MapEditorSpawnPointData[] spawns, string role)
    {
        if (spawns == null) return null;
        for (int i = 0; i < spawns.Length; i++)
        {
            if (spawns[i] != null && NormalizeRole(spawns[i].role) == role) return spawns[i];
        }
        return null;
    }

    private MapEditorSpawnPointData FindFirstWalkableSpawn(MapEditorSpawnPointData[] spawns)
    {
        if (spawns == null) return null;
        for (int i = 0; i < spawns.Length; i++)
        {
            MapEditorSpawnPointData spawn = spawns[i];
            if (spawn != null && manager.HasPlaytestGroundAt(spawn.x, spawn.y) && !manager.HasPlaytestCollisionAt(spawn.x, spawn.y)) return spawn;
        }
        return null;
    }

    private static string NormalizeRole(string role)
    {
        return string.Equals(role, "Seeker", System.StringComparison.OrdinalIgnoreCase) ? "Seeker" : "Runner";
    }

    private static bool Contains(Vector2 topLeft, int width, int height, Vector2 point)
    {
        return point.x >= topLeft.x && point.y >= topLeft.y && point.x < topLeft.x + width && point.y < topLeft.y + height;
    }

    private static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, TextAnchor alignment, int fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = obj.GetComponent<Text>();
        text.font = MapEditorFontProvider.Default;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.15f, 0.32f, 0.5f, 1f);
        Button button = obj.GetComponent<Button>();
        button.onClick.AddListener(action);
        Text label = CreateText(obj.transform, "Label", Vector2.zero, size, TextAnchor.MiddleCenter, 12);
        label.text = name;
        label.raycastTarget = false;
        return button;
    }

    private static string L(string korean, string english)
    {
        return MapEditorLocalization.Choose(korean, english);
    }

    private static void DestroyRuntimeObject(GameObject obj)
    {
        if (obj != null) Object.Destroy(obj);
    }
}

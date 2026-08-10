using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class MapEditorChunkRenderingSmokeTest
{
    [MenuItem("Tools/MapEditor/Run Chunk Rendering Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        MapEditorManager manager = UnityEngine.Object.FindFirstObjectByType<MapEditorManager>();
        Require(manager != null, "SampleScene is missing MapEditorManager.");

        GridGenerator generator = manager.GetComponent<GridGenerator>();
        Require(generator != null && generator.gridParent != null, "SampleScene is missing GridGenerator references.");

        manager.CurrentMapData.SetTileOnLayer(
            2,
            3,
            MapEditorLayerType.Ground,
            MapEditorManager.CustomColorTileId,
            Color.green,
            string.Empty,
            -1,
            0,
            false,
            false);

        Stopwatch timer = Stopwatch.StartNew();
        UnityEngine.Debug.Log("Chunk smoke: resizing map to 256x256.");
        manager.ResizeMap(256, 256, false);
        timer.Stop();
        UnityEngine.Debug.Log($"Chunk smoke: resize completed in {timer.ElapsedMilliseconds}ms.");

        Require(generator.UsesChunkRendering, "256 x 256 map did not switch to chunk rendering.");
        Require(
            manager.CurrentMapData.GetTile(2, 3, MapEditorLayerType.Ground) == MapEditorManager.CustomColorTileId
            && manager.CurrentMapData.GetColor(2, 3, MapEditorLayerType.Ground) == Color.green,
            "Growing the map replaced existing tile data instead of preserving it.");
        Require(generator.ChunkRenderer != null && generator.ChunkRenderer.IsActive,
            "Chunk renderer was not activated.");
        Require(generator.ChunkRenderer.ChunkCount == 256,
            $"Expected 256 chunks for a 256 x 256 map, got {generator.ChunkRenderer.ChunkCount}.");
        Require(generator.gridParent.GetComponentsInChildren<GridCell>(true).Length == 0,
            "Large map still created one GridCell object per map tile.");
        Require(generator.gridParent.GetComponentsInChildren<RectTransform>(true).Length < 400,
            "Large map created too many UI objects.");
        Require(timer.Elapsed.TotalSeconds < 10d,
            $"Large map generation took too long: {timer.Elapsed.TotalSeconds:F2}s.");

        MapEditorGridInputSurface inputSurface = generator.gridParent.GetComponent<MapEditorGridInputSurface>();
        EventSystem eventSystem = EventSystem.current != null
            ? EventSystem.current
            : UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        RectTransform gridRect = generator.gridParent as RectTransform;
        RectTransform viewportRect = gridRect == null ? null : gridRect.parent as RectTransform;
        Require(inputSurface != null && eventSystem != null && gridRect != null && viewportRect != null,
            "Chunk map zoom input surface is missing.");
        gridRect.localScale = Vector3.one;
        var scrollEvent = new PointerEventData(eventSystem)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, viewportRect.TransformPoint(viewportRect.rect.center)),
            scrollDelta = Vector2.up
        };
        inputSurface.OnScroll(scrollEvent);
        Require(gridRect.localScale.x > 1f, "Mouse wheel did not zoom the chunk-rendered map.");
        float zoomedScale = gridRect.localScale.x;
        scrollEvent.scrollDelta = Vector2.down;
        inputSurface.OnScroll(scrollEvent);
        Require(gridRect.localScale.x < zoomedScale, "Mouse wheel did not zoom out the chunk-rendered map.");

        manager.SetBrushLayerRole(MapEditorLayerType.Ground);
        manager.SetBrushTool();
        manager.paintWholeTile = true;
        manager.useSelectedColor = true;
        manager.selectedImageBrush = null;
        manager.selectedColor = Color.magenta;
        manager.HandleVirtualPointerDown(200, 220, 8, 8);
        manager.HandleVirtualPointerUp(200, 220, 8, 8);
        UnityEngine.Debug.Log("Chunk smoke: virtual pointer paint completed.");

        Require(manager.CurrentMapData.GetTile(200, 220, manager.ActiveLayer) != -1,
            "Virtual chunk input did not paint the target map tile.");

        Stopwatch renderTimer = Stopwatch.StartNew();
        generator.ChunkRenderer.RenderOneNow();
        renderTimer.Stop();
        Require(renderTimer.Elapsed.TotalSeconds < 1d,
            $"One chunk took too long to render: {renderTimer.Elapsed.TotalSeconds:F2}s.");
        UnityEngine.Debug.Log($"Chunk smoke: one dirty chunk rendered in {renderTimer.ElapsedMilliseconds}ms.");
        UnityEngine.Debug.Log(
            $"MapEditor chunk rendering smoke test passed. " +
            $"256x256 used {generator.ChunkRenderer.ChunkCount} chunks and generated in {timer.ElapsedMilliseconds}ms.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

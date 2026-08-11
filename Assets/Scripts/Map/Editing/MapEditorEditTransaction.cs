using System;
using System.Collections.Generic;
using UnityEngine;

public struct TileEditAction
{
    public int x;
    public int y;
    public int beforeTileId;
    public int afterTileId;
    public Color beforeColor;
    public Color afterColor;
    public Sprite beforeSprite;
    public Sprite afterSprite;
    public string beforeImagePath;
    public string afterImagePath;
    public int beforeImageIndex;
    public int afterImageIndex;
    public int beforeImageRotation;
    public int afterImageRotation;
    public bool beforeImageFlipX;
    public bool afterImageFlipX;
    public bool beforeImageFlipY;
    public bool afterImageFlipY;
    public MapEditorLayerType beforeLayer;
    public MapEditorLayerType afterLayer;
    public MapTilePixelData beforePixelData;
    public MapTilePixelData afterPixelData;

    public TileEditAction(int x, int y, int beforeTileId, int afterTileId, Color beforeColor, Color afterColor, Sprite beforeSprite, Sprite afterSprite, string beforeImagePath, string afterImagePath, int beforeImageIndex, int afterImageIndex, int beforeImageRotation, int afterImageRotation, bool beforeImageFlipX, bool afterImageFlipX, bool beforeImageFlipY, bool afterImageFlipY)
        : this(x, y, beforeTileId, afterTileId, beforeColor, afterColor, beforeSprite, afterSprite, beforeImagePath, afterImagePath, beforeImageIndex, afterImageIndex, beforeImageRotation, afterImageRotation, beforeImageFlipX, afterImageFlipX, beforeImageFlipY, afterImageFlipY, MapData.InferLayerFromTile(beforeTileId), MapData.InferLayerFromTile(afterTileId))
    {
    }

    public TileEditAction(int x, int y, int beforeTileId, int afterTileId, Color beforeColor, Color afterColor, Sprite beforeSprite, Sprite afterSprite, string beforeImagePath, string afterImagePath, int beforeImageIndex, int afterImageIndex, int beforeImageRotation, int afterImageRotation, bool beforeImageFlipX, bool afterImageFlipX, bool beforeImageFlipY, bool afterImageFlipY, MapEditorLayerType beforeLayer, MapEditorLayerType afterLayer)
    {
        this.x = x;
        this.y = y;
        this.beforeTileId = beforeTileId;
        this.afterTileId = afterTileId;
        this.beforeColor = beforeColor;
        this.afterColor = afterColor;
        this.beforeSprite = beforeSprite;
        this.afterSprite = afterSprite;
        this.beforeImagePath = beforeImagePath;
        this.afterImagePath = afterImagePath;
        this.beforeImageIndex = beforeImageIndex;
        this.afterImageIndex = afterImageIndex;
        this.beforeImageRotation = beforeImageRotation;
        this.afterImageRotation = afterImageRotation;
        this.beforeImageFlipX = beforeImageFlipX;
        this.afterImageFlipX = afterImageFlipX;
        this.beforeImageFlipY = beforeImageFlipY;
        this.afterImageFlipY = afterImageFlipY;
        this.beforeLayer = beforeLayer;
        this.afterLayer = afterLayer;
        beforePixelData = null;
        afterPixelData = null;
    }

    public TileEditAction(int x, int y, int beforeTileId, int afterTileId, Color beforeColor, Color afterColor, Sprite beforeSprite, Sprite afterSprite, string beforeImagePath, string afterImagePath, int beforeImageIndex, int afterImageIndex, int beforeImageRotation, int afterImageRotation, bool beforeImageFlipX, bool afterImageFlipX, bool beforeImageFlipY, bool afterImageFlipY, MapTilePixelData beforePixelData, MapTilePixelData afterPixelData)
        : this(x, y, beforeTileId, afterTileId, beforeColor, afterColor, beforeSprite, afterSprite, beforeImagePath, afterImagePath, beforeImageIndex, afterImageIndex, beforeImageRotation, afterImageRotation, beforeImageFlipX, afterImageFlipX, beforeImageFlipY, afterImageFlipY)
    {
        this.beforePixelData = beforePixelData == null ? null : beforePixelData.Clone();
        this.afterPixelData = afterPixelData == null ? null : afterPixelData.Clone();
    }

    public TileEditAction(int x, int y, int beforeTileId, int afterTileId, Color beforeColor, Color afterColor, Sprite beforeSprite, Sprite afterSprite, string beforeImagePath, string afterImagePath, int beforeImageIndex, int afterImageIndex, int beforeImageRotation, int afterImageRotation, bool beforeImageFlipX, bool afterImageFlipX, bool beforeImageFlipY, bool afterImageFlipY, MapEditorLayerType beforeLayer, MapEditorLayerType afterLayer, MapTilePixelData beforePixelData, MapTilePixelData afterPixelData)
        : this(x, y, beforeTileId, afterTileId, beforeColor, afterColor, beforeSprite, afterSprite, beforeImagePath, afterImagePath, beforeImageIndex, afterImageIndex, beforeImageRotation, afterImageRotation, beforeImageFlipX, afterImageFlipX, beforeImageFlipY, afterImageFlipY, beforeLayer, afterLayer)
    {
        this.beforePixelData = beforePixelData == null ? null : beforePixelData.Clone();
        this.afterPixelData = afterPixelData == null ? null : afterPixelData.Clone();
    }
}

public class MapEditTransaction
{
    private readonly List<TileEditAction> orderedActions = new List<TileEditAction>();
    private readonly Dictionary<TileActionKey, int> actionIndices = new Dictionary<TileActionKey, int>();
    private readonly List<TransactionSideEffect> sideEffects = new List<TransactionSideEffect>();

    public int Count => orderedActions.Count + sideEffects.Count;

    public void AddOrUpdate(TileEditAction action)
    {
        TileActionKey key = new TileActionKey(action.x, action.y, action.afterLayer);

        if (actionIndices.TryGetValue(key, out int index))
        {
            TileEditAction existing = orderedActions[index];
            orderedActions[index] = new TileEditAction(
                existing.x,
                existing.y,
                existing.beforeTileId,
                action.afterTileId,
                existing.beforeColor,
                action.afterColor,
                existing.beforeSprite,
                action.afterSprite,
                existing.beforeImagePath,
                action.afterImagePath,
                existing.beforeImageIndex,
                action.afterImageIndex,
                existing.beforeImageRotation,
                action.afterImageRotation,
                existing.beforeImageFlipX,
                action.afterImageFlipX,
                existing.beforeImageFlipY,
                action.afterImageFlipY,
                existing.beforeLayer,
                action.afterLayer,
                existing.beforePixelData,
                action.afterPixelData
            );
            return;
        }

        actionIndices[key] = orderedActions.Count;
        orderedActions.Add(action);
    }

    public void AddSideEffect(Action undo, Action redo)
    {
        if (undo == null && redo == null)
        {
            return;
        }

        sideEffects.Add(new TransactionSideEffect(undo, redo));
    }

    public void ApplyBefore(Action<TileEditAction, bool> apply)
    {
        for (int i = orderedActions.Count - 1; i >= 0; i--)
        {
            apply(orderedActions[i], false);
        }


        for (int i = sideEffects.Count - 1; i >= 0; i--)
        {
            sideEffects[i].undo?.Invoke();
        }
    }

    public void ApplyAfter(Action<TileEditAction, bool> apply)
    {
        for (int i = 0; i < orderedActions.Count; i++)
        {
            apply(orderedActions[i], true);
        }


        for (int i = 0; i < sideEffects.Count; i++)
        {
            sideEffects[i].redo?.Invoke();
        }
    }

    private readonly struct TileActionKey : IEquatable<TileActionKey>
    {
        private readonly int x;
        private readonly int y;
        private readonly MapEditorLayerType layer;

        public TileActionKey(int x, int y, MapEditorLayerType layer)
        {
            this.x = x;
            this.y = y;
            this.layer = layer;
        }

        public bool Equals(TileActionKey other)
        {
            return x == other.x && y == other.y && layer == other.layer;
        }

        public override bool Equals(object obj)
        {
            return obj is TileActionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = x;
                hash = (hash * 397) ^ y;
                hash = (hash * 397) ^ (int)layer;
                return hash;
            }
        }
    }

    private readonly struct TransactionSideEffect
    {
        public readonly Action undo;
        public readonly Action redo;

        public TransactionSideEffect(Action undo, Action redo)
        {
            this.undo = undo;
            this.redo = redo;
        }
    }
}

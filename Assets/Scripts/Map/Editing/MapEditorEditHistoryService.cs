using System;
using System.Collections.Generic;

public sealed class MapEditorEditHistoryService
{
    private readonly Stack<MapEditTransaction> undoStack = new Stack<MapEditTransaction>();
    private readonly Stack<MapEditTransaction> redoStack = new Stack<MapEditTransaction>();

    private MapEditTransaction activeTransaction;

    public bool HasActiveTransaction => activeTransaction != null;

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
        activeTransaction = null;
    }

    public void BeginTransaction()
    {
        if (activeTransaction == null)
        {
            activeTransaction = new MapEditTransaction();
        }
    }

    public void CommitTransaction(Action refresh)
    {
        if (activeTransaction == null)
        {
            return;
        }

        if (activeTransaction.Count > 0)
        {
            undoStack.Push(activeTransaction);
            redoStack.Clear();
        }

        activeTransaction = null;
        refresh?.Invoke();
    }

    public void Record(TileEditAction action)
    {
        if (activeTransaction != null)
        {
            activeTransaction.AddOrUpdate(action);
            return;
        }

        MapEditTransaction transaction = new MapEditTransaction();
        transaction.AddOrUpdate(action);
        undoStack.Push(transaction);
        redoStack.Clear();
    }

    public void RecordSideEffect(Action undo, Action redo)
    {
        if (activeTransaction != null)
        {
            activeTransaction.AddSideEffect(undo, redo);
            return;
        }

        MapEditTransaction transaction = new MapEditTransaction();
        transaction.AddSideEffect(undo, redo);
        undoStack.Push(transaction);
        redoStack.Clear();
    }

    public void Undo(Action<TileEditAction, bool> applyAction, Action refresh)
    {
        if (undoStack.Count == 0)
        {
            return;
        }

        MapEditTransaction transaction = undoStack.Pop();
        transaction.ApplyBefore(applyAction);
        redoStack.Push(transaction);
        refresh?.Invoke();
    }

    public void Redo(Action<TileEditAction, bool> applyAction, Action refresh)
    {
        if (redoStack.Count == 0)
        {
            return;
        }

        MapEditTransaction transaction = redoStack.Pop();
        transaction.ApplyAfter(applyAction);
        undoStack.Push(transaction);
        refresh?.Invoke();
    }
}

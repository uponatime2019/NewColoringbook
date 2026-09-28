using System;
using System.Collections.Generic;

namespace NewColoringbook.Services
{
    /// <summary>One reversible edit. Redo() must re-apply, Undo() must revert.</summary>
    public abstract class UndoOp
    {
        public abstract string Label { get; }
        public abstract void Undo();
        public abstract void Redo();
    }

    /// <summary>Simple op built from a pair of closures; the do-action runs on Push.</summary>
    public sealed class ActionOp : UndoOp
    {
        private readonly string _label;
        private readonly Action _undo;
        private readonly Action _redo;

        public ActionOp(string label, Action undo, Action redo)
        {
            _label = label;
            _undo = undo;
            _redo = redo;
        }

        public override string Label => _label;
        public override void Undo()
        {
            try { _undo(); }
            catch (Exception ex) { AppLog.Error($"Undo '{_label}' failed", ex); }
        }

        public override void Redo()
        {
            try { _redo(); }
            catch (Exception ex) { AppLog.Error($"Redo '{_label}' failed", ex); }
        }
    }

    /// <summary>Bounded undo/redo stacks with a change notification for button states.</summary>
    public sealed class History
    {
        private readonly Stack<UndoOp> _undo = new();
        private readonly Stack<UndoOp> _redo = new();

        public int Capacity { get; set; } = 200;
        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;
        public string? NextUndoLabel => _undo.Count > 0 ? _undo.Peek().Label : null;
        public string? NextRedoLabel => _redo.Count > 0 ? _redo.Peek().Label : null;

        public event Action? Changed;

        /// <summary>Apply the op and record it (clears the redo branch).</summary>
        public void Push(UndoOp op)
        {
            try
            {
                op.Redo();
                _undo.Push(op);
                _redo.Clear();
                while (_undo.Count > Capacity) TrimOldest();
                Changed?.Invoke();
            }
            catch (Exception ex)
            {
                AppLog.Error("History.Push failed", ex);
            }
        }

        /// <summary>Record an already-applied change (e.g. bulk restore) without re-running it.</summary>
        public void PushApplied(UndoOp op)
        {
            try
            {
                _undo.Push(op);
                _redo.Clear();
                while (_undo.Count > Capacity) TrimOldest();
                Changed?.Invoke();
            }
            catch (Exception ex)
            {
                AppLog.Error("History.PushApplied failed", ex);
            }
        }

        public bool Undo()
        {
            try
            {
                if (_undo.Count == 0) return false;
                var op = _undo.Pop();
                try { op.Undo(); } finally { _redo.Push(op); }
                Changed?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Error("History.Undo failed", ex);
                return false;
            }
        }

        public bool Redo()
        {
            try
            {
                if (_redo.Count == 0) return false;
                var op = _redo.Pop();
                try { op.Redo(); } finally { _undo.Push(op); }
                Changed?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Error("History.Redo failed", ex);
                return false;
            }
        }

        public void Clear()
        {
            try
            {
                _undo.Clear();
                _redo.Clear();
                Changed?.Invoke();
            }
            catch (Exception ex)
            {
                AppLog.Error("History.Clear failed", ex);
            }
        }

        private void TrimOldest()
        {
            try
            {
                // Stack is LIFO; drop the deepest item by pouring into a temp stack.
                var keep = new Stack<UndoOp>();
                while (_undo.Count > 1) keep.Push(_undo.Pop());
                if (_undo.Count > 0) _undo.Pop();
                while (keep.Count > 0) _undo.Push(keep.Pop());
            }
            catch { }
        }
    }
}


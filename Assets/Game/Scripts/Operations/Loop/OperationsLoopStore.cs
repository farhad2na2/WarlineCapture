using System;
using System.Collections.Generic;

namespace Game.Operations.Loop
{
    public sealed class OperationsLoopStore
    {
        private OperationsLoopDocument _committed = new();
        private OperationsLoopDocument _pending;
        private readonly Dictionary<string, string> _blobs = new(StringComparer.Ordinal);
        private string _stagedId = string.Empty;
        private string _stagedText = string.Empty;
        private string _pendingBlobId = string.Empty;
        private string _pendingBlobText = string.Empty;

        public bool HasPending => _pending != null;
        public bool HasStaged => _stagedText.Length > 0;
        public OperationsLoopDocument Committed => _committed.Copy();

        public bool TryPending(out OperationsLoopDocument pending)
        {
            pending = _pending == null ? null : _pending.Copy();
            return _pending != null;
        }

        public void Stage(string id, string text)
        {
            if (HasPending)
                throw new InvalidOperationException("loop_pending");
            _stagedId = id ?? string.Empty;
            _stagedText = text ?? string.Empty;
        }

        public void Begin(OperationsLoopDocument next, bool adoptStaged)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));
            if (HasPending)
                throw new InvalidOperationException("loop_pending");
            _pending = next.Copy();
            if (!adoptStaged)
                return;
            if (_stagedText.Length == 0)
                throw new InvalidOperationException("checkpoint_not_staged");
            _pendingBlobId = _stagedId;
            _pendingBlobText = _stagedText;
            _stagedId = string.Empty;
            _stagedText = string.Empty;
        }

        public void Complete()
        {
            if (_pending == null)
                throw new InvalidOperationException("loop_not_pending");
            _committed = _pending;
            _pending = null;
            if (_pendingBlobText.Length == 0)
                return;
            _blobs[_pendingBlobId] = _pendingBlobText;
            _pendingBlobId = string.Empty;
            _pendingBlobText = string.Empty;
        }

        public void Abandon()
        {
            _pending = null;
            _pendingBlobId = string.Empty;
            _pendingBlobText = string.Empty;
            _stagedId = string.Empty;
            _stagedText = string.Empty;
        }

        public bool TryBlob(string id, out string text)
        {
            if (string.IsNullOrEmpty(id))
            {
                text = string.Empty;
                return false;
            }

            return _blobs.TryGetValue(id, out text);
        }

        public void ReplaceBlob(string id, string text)
        {
            if (!_blobs.ContainsKey(id))
                throw new InvalidOperationException("checkpoint_missing");
            _blobs[id] = text ?? string.Empty;
        }

        public void CopyBlobs(Dictionary<string, string> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            foreach (KeyValuePair<string, string> pair in _blobs)
                destination[pair.Key] = pair.Value;
        }

        public void LoadCommitted(OperationsLoopDocument document, IDictionary<string, string> blobs)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            _pending = null;
            _pendingBlobId = string.Empty;
            _pendingBlobText = string.Empty;
            _stagedId = string.Empty;
            _stagedText = string.Empty;
            _committed = document.Copy();
            _blobs.Clear();
            if (blobs == null)
                return;
            foreach (KeyValuePair<string, string> pair in blobs)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Key.IndexOf('/') >= 0 || pair.Key.IndexOf('\\') >= 0)
                    throw new InvalidOperationException("checkpoint_id");
                _blobs[pair.Key] = pair.Value ?? string.Empty;
            }
        }
    }
}

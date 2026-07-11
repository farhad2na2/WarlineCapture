using System;

namespace Game.Composition
{
    internal sealed class StaticMapPresentationDetachedOperation
    {
        private IStaticMapPresentationSceneOperation _operation;
        private string _path;
        private bool _wasLoad;
        private byte _unloadAttemptCount;
        public bool IsPending => !string.IsNullOrEmpty(_path);
        public string Status { get; private set; }
        public string Failure { get; private set; }
        public void Capture(
            IStaticMapPresentationSceneOperation operation,
            bool wasLoad,
            byte retryCount,
            string path)
        {
            _operation = operation;
            _path = path;
            _wasLoad = wasLoad;
            _unloadAttemptCount = wasLoad ? (byte)0 : (byte)(retryCount + 1);
            Status = "Waiting for previous scene operation";
            Failure = null;
        }
        public bool BlocksBind(IStaticMapPresentationSceneApi sceneApi)
        {
            if (!IsPending)
                return false;
            if (!string.IsNullOrEmpty(Failure))
                return true;
            if (_operation != null && !_operation.IsDone)
            {
                Status = "Waiting for previous scene operation";
                return true;
            }
            _operation = null;
            if (!sceneApi.IsLoaded(_path))
            {
                Clear();
                return false;
            }
            if (_wasLoad)
            {
                _wasLoad = false;
                _unloadAttemptCount = 0;
            }
            else if (_unloadAttemptCount >= 2)
            {
                Failure = $"Detached scene unload failed twice: {_path}";
                Status = Failure;
                return true;
            }
            return StartUnload(sceneApi);
        }
        private bool StartUnload(IStaticMapPresentationSceneApi sceneApi)
        {
            _unloadAttemptCount++;
            try
            {
                _operation = sceneApi.Unload(_path);
            }
            catch (Exception exception)
            {
                return HandleStartFailure(exception.Message);
            }
            if (_operation == null)
                return HandleStartFailure(null);
            Status = "Unloading previous map presentation";
            return true;
        }
        private bool HandleStartFailure(string detail)
        {
            _operation = null;
            if (_unloadAttemptCount < 2)
            {
                Status = "Retrying previous map presentation unload";
                return true;
            }
            string suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" ({detail})";
            Failure = $"Detached scene unload failed to start twice: {_path}{suffix}";
            Status = Failure;
            return true;
        }
        private void Clear()
        {
            _operation = null;
            _path = null;
            _wasLoad = false;
            _unloadAttemptCount = 0;
            Status = null;
            Failure = null;
        }
    }
}

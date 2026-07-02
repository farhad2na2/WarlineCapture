using System;

namespace Game.Runtime
{
    [Serializable]
    public readonly struct SaveSlotInfo
    {
        public string SlotId { get; }
        public string FileName { get; }
        public bool Exists { get; }

        public SaveSlotInfo(string slotId, string fileName, bool exists)
        {
            SlotId = slotId;
            FileName = fileName;
            Exists = exists;
        }
    }
}

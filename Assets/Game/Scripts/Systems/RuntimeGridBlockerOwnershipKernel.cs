using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    internal readonly struct RuntimeGridBlockerOwnershipRecord
    {
        internal readonly int Id;
        internal readonly Entity Entity;
        internal readonly int2 OriginCell;
        internal readonly int2 SizeCells;

        internal RuntimeGridBlockerOwnershipRecord(int id, Entity entity, int2 originCell, int2 sizeCells)
        {
            Id = id;
            Entity = entity;
            OriginCell = originCell;
            SizeCells = sizeCells;
        }
    }

    // Shared data owner for runtime blocker membership and cell occupancy. The
    // presentation helper owns visuals; both it and the headless host use this
    // same record/removal policy.
    internal sealed class RuntimeGridBlockerOwnershipKernel
    {
        private readonly Dictionary<int, RuntimeGridBlockerOwnershipRecord> _records = new();
        private int[] _cellCounts;
        private int _width;
        private int _height;
        private int _nextId = 1;

        internal int Count => _records.Count;

        internal void InitializeGrid(int width, int height)
        {
            if (width < 1 || height < 1)
                throw new ArgumentOutOfRangeException(nameof(width), "Positive runtime blocker grid dimensions required");

            _width = width;
            _height = height;
            _cellCounts = new int[checked(width * height)];
        }

        internal int AllocateId()
        {
            if (_nextId == int.MaxValue)
                throw new InvalidOperationException("Runtime blocker identity space exhausted");
            return _nextId++;
        }

        internal void Register(int id, Entity entity, int2 originCell, int2 sizeCells)
        {
            if (_cellCounts == null || _width < 1 || _height < 1)
                throw new InvalidOperationException("Runtime blocker grid must be initialized first");
            if (id <= 0 || entity == Entity.Null || sizeCells.x < 1 || sizeCells.y < 1 ||
                originCell.x < 0 || originCell.y < 0 || originCell.x > _width - sizeCells.x ||
                originCell.y > _height - sizeCells.y)
                throw new ArgumentOutOfRangeException(nameof(originCell), "Runtime blocker must have a unique id and a positive in-grid footprint");
            if (_records.ContainsKey(id))
                throw new InvalidOperationException("Runtime blocker id already registered: " + id);

            var record = new RuntimeGridBlockerOwnershipRecord(id, entity, originCell, sizeCells);
            _records.Add(id, record);
            AdjustCells(record, +1);
        }

        internal IReadOnlyList<int> FindOverlappingIds(int2 originCell, int2 sizeCells)
        {
            var ids = new List<int>();
            if (sizeCells.x < 1 || sizeCells.y < 1)
                return ids;

            int right = originCell.x + sizeCells.x;
            int top = originCell.y + sizeCells.y;
            foreach (KeyValuePair<int, RuntimeGridBlockerOwnershipRecord> entry in _records)
            {
                RuntimeGridBlockerOwnershipRecord record = entry.Value;
                if (originCell.x < record.OriginCell.x + record.SizeCells.x &&
                    record.OriginCell.x < right &&
                    originCell.y < record.OriginCell.y + record.SizeCells.y &&
                    record.OriginCell.y < top)
                    ids.Add(entry.Key);
            }

            return ids;
        }

        internal bool TryRemove(int id, bool adjustOccupancy, out RuntimeGridBlockerOwnershipRecord record)
        {
            if (!_records.TryGetValue(id, out record))
                return false;

            _records.Remove(id);
            if (adjustOccupancy)
                AdjustCells(record, -1);
            return true;
        }

        internal bool IsRuntimeBlockerCell(int x, int y, int gridWidth, int gridHeight)
        {
            if ((uint)x >= (uint)gridWidth || (uint)y >= (uint)gridHeight ||
                _cellCounts == null || _width != gridWidth || _height != gridHeight)
                return false;

            return _cellCounts[GridUtils.CellToIndex(new int2(x, y), gridWidth)] > 0;
        }

        internal void Clear()
        {
            _records.Clear();
            _cellCounts = null;
            _width = 0;
            _height = 0;
        }

        private void AdjustCells(RuntimeGridBlockerOwnershipRecord record, int delta)
        {
            for (int y = record.OriginCell.y; y < record.OriginCell.y + record.SizeCells.y; y++)
            {
                for (int x = record.OriginCell.x; x < record.OriginCell.x + record.SizeCells.x; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), _width);
                    int count = _cellCounts[index] + delta;
                    _cellCounts[index] = count > 0 ? count : 0;
                }
            }
        }
    }
}

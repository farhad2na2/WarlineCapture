using System;
using System.Collections.Generic;
using Game.Runtime;

internal static class MapBuildingPlacementSequenceKernelHarness
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static int Main()
    {
        var sequence = new MapBuildingPlacementSequenceKernel();
        var production = new HashSet<int> { 35, 37 };
        var prioritySucceeded = new HashSet<int>();
        var attempts = new List<int>();
        var attemptsByIndex = new Dictionary<int, int>();

        for (int loopIndex = 0; loopIndex < 40; loopIndex++)
        {
            if (!production.Contains(loopIndex)) continue;
            bool success = Try(loopIndex);
            if (success)
            {
                sequence.RecordPrioritySuccess(loopIndex);
                prioritySucceeded.Add(loopIndex);
            }
        }
        sequence.CompletePriorityPass();
        Need(prioritySucceeded.SetEquals(new[] { 37 }), "Only successful out-of-order priority rows must be retained");

        int processed = 0;
        Need(sequence.TryBeginSequentialAttempt(40, 32, processed, out int sequentialIndex) && sequentialIndex == 0,
            "First update must begin at source index zero");
        while (sequence.TryBeginSequentialAttempt(40, 32, processed, out sequentialIndex))
        {
            if (!sequence.WasPrioritySuccessful(sequentialIndex)) Try(sequentialIndex);
            sequence.CompleteSequentialAttempt(sequentialIndex);
            processed++;
        }
        Need(processed == 32 && !sequence.QueueWhenExhausted(40) && sequence.NextPlacementIndex == 32,
            "A partial source batch must remain queued for later updates");

        processed = 0;
        while (sequence.TryBeginSequentialAttempt(40, 32, processed, out sequentialIndex))
        {
            if (!sequence.WasPrioritySuccessful(sequentialIndex)) Try(sequentialIndex);
            sequence.CompleteSequentialAttempt(sequentialIndex);
            processed++;
        }
        Need(processed == 8 && sequence.QueueWhenExhausted(40) && sequence.IsQueued,
            "Queue completion follows cursor exhaustion even when a placement failed");
        Need(attemptsByIndex[35] == 2 && attemptsByIndex[37] == 1 && attemptsByIndex[34] == 1,
            "Failed priority retries in its sequential slot; successful priority skips once; failed sequential does not retry");

        var expected = new List<int> { 35, 37 };
        for (int loopIndex = 0; loopIndex < 32; loopIndex++) expected.Add(loopIndex);
        for (int loopIndex = 32; loopIndex < 40; loopIndex++) if (loopIndex != 37) expected.Add(loopIndex);
        Need(attempts.Count == expected.Count, "Unexpected number of actual policy attempts");
        for (int i = 0; i < attempts.Count; i++)
            Need(attempts[i] == expected[i], "Priority/batch order mismatch at attempt " + i);

        var empty = new MapBuildingPlacementSequenceKernel();
        empty.QueueEmpty();
        Need(empty.IsQueued, "An empty original placement config must queue without creating a batch");

        var exception = new MapBuildingPlacementSequenceKernel();
        exception.CompletePriorityPass();
        Need(exception.TryBeginSequentialAttempt(3, 32, 0, out int retryIndex) && retryIndex == 0,
            "Exception probe must begin at the current source index");
        Need(exception.NextPlacementIndex == 0,
            "A throwing spawn must leave the current source index available for the next update");
        exception.CompleteSequentialAttempt(retryIndex);
        Need(exception.NextPlacementIndex == 1,
            "A completed attempt advances exactly one source index");
        Need(exception.TryBeginSequentialAttempt(4, 32, 0, out int addedIndex) && addedIndex == 1,
            "A changed config count must remain observable without rebinding or rejecting it");
        exception.CompleteSequentialAttempt(addedIndex);
        Need(exception.TryBeginSequentialAttempt(2, 32, 1, out _) == false && exception.QueueWhenExhausted(2),
            "A shrunken config stops the current pass and queues once the original cursor is exhausted");

        var emptied = new MapBuildingPlacementSequenceKernel();
        emptied.CompletePriorityPass();
        Need(emptied.TryBeginSequentialAttempt(3, 32, 0, out int priorIndex) && priorIndex == 0,
            "Empty-config probe must first be in a normal sequential pass");
        emptied.CompleteSequentialAttempt(priorIndex);
        emptied.QueueEmpty();
        Need(emptied.IsQueued,
            "A config becoming empty must queue even after prior indices were processed");

        Console.WriteLine("[MapBuildingPlacementSequenceKernelHarness] Passed attempts=" + attempts.Count +
            " batches=32+8 priorityRows=2 retainedPrioritySuccesses=1 failedPriorityRetried=1 failedSequentialRetried=0" +
            " exceptionCursor=preserved dynamicCount=accepted");
        return 0;

        bool Try(int index)
        {
            attempts.Add(index);
            attemptsByIndex[index] = attemptsByIndex.TryGetValue(index, out int count) ? count + 1 : 1;
            if (index == 35 && attemptsByIndex[index] == 1) return false;
            return index != 34;
        }
    }
}

using System;
using System.Globalization;

namespace Game.UI.Runtime
{
    public sealed class NarrativeDialogueRevealSystemHelper
    {
        private float[] revealTimes = Array.Empty<float>();
        private int visibleCharacterCount;
        private float duration;

        public int VisibleCharacterCount => visibleCharacterCount;
        public float Duration => duration;

        public void Prepare(
            string text,
            float availableSeconds,
            float charactersPerSecond,
            float commaPauseSeconds,
            float clausePauseSeconds,
            float sentencePauseSeconds,
            float ellipsisPauseSeconds,
            bool instant)
        {
            text ??= string.Empty;
            EnsureCapacity(text.Length);
            visibleCharacterCount = 0;
            float cursor = 0f;
            bool insideTag = false;
            float step = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == '<' && text.IndexOf('>', i + 1) >= 0)
                {
                    insideTag = true;
                    continue;
                }

                if (insideTag)
                {
                    if (character == '>')
                        insideTag = false;
                    continue;
                }

                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(text, i);
                bool combiningMark = category == UnicodeCategory.NonSpacingMark ||
                                     category == UnicodeCategory.SpacingCombiningMark ||
                                     category == UnicodeCategory.EnclosingMark;
                if (!combiningMark)
                {
                    cursor += step;
                    revealTimes[visibleCharacterCount++] = cursor;
                }

                if (character == '.' && IsEllipsisMember(text, i))
                {
                    if (i == 0 || text[i - 1] != '.')
                        cursor += ellipsisPauseSeconds;
                }
                else
                {
                    cursor += character switch
                    {
                        ',' => commaPauseSeconds,
                        ':' or ';' => clausePauseSeconds,
                        '.' or '?' or '!' => sentencePauseSeconds,
                        _ => 0f
                    };
                }
            }

            duration = instant ? 0f : cursor;
            if (!instant && availableSeconds > 0f && duration > availableSeconds)
            {
                float scale = availableSeconds / duration;
                for (int i = 0; i < visibleCharacterCount; i++)
                    revealTimes[i] *= scale;
                duration = availableSeconds;
            }

            if (instant)
            {
                for (int i = 0; i < visibleCharacterCount; i++)
                    revealTimes[i] = 0f;
            }
        }

        public int GetVisibleCharacterCount(float elapsedSeconds)
        {
            if (visibleCharacterCount == 0)
                return 0;

            int low = 0;
            int high = visibleCharacterCount;
            while (low < high)
            {
                int middle = low + ((high - low) >> 1);
                if (revealTimes[middle] <= elapsedSeconds)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        private void EnsureCapacity(int required)
        {
            if (revealTimes.Length >= required)
                return;

            int capacity = Math.Max(32, revealTimes.Length);
            while (capacity < required)
                capacity *= 2;
            Array.Resize(ref revealTimes, capacity);
        }

        private static bool IsEllipsisMember(string text, int index)
        {
            int runStart = index;
            while (runStart > 0 && text[runStart - 1] == '.')
                runStart--;
            int runEnd = index;
            while (runEnd + 1 < text.Length && text[runEnd + 1] == '.')
                runEnd++;
            return runEnd - runStart + 1 >= 3;
        }
    }
}

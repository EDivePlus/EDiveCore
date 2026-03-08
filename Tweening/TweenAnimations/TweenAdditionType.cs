using System;
using DG.Tweening;

namespace EDIVE.Tweening
{
    public enum TweenAdditionType
    {
        Append,
        Prepend,
        Insert,
        Join
    }

    public static class TweenAdditionTypeExtensions
    {
        public static string GetSummaryPrefix(this TweenAdditionType operation, float insertionPosition = 0f)
        {
            switch (operation)
            {
                case TweenAdditionType.Append:
                case TweenAdditionType.Prepend:
                case TweenAdditionType.Join:
                    return $"{operation}";
                case TweenAdditionType.Insert:
                    return $"{operation} ({insertionPosition}s)";
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static Sequence AddToSequence(this Sequence sequence, Tween tween, TweenAdditionType operation, float insertionPosition = 0f)
        {
            if (sequence == null || tween == null)
                return sequence;

            switch (operation)
            {
                case TweenAdditionType.Append:
                    sequence.Append(tween);
                    break;
                case TweenAdditionType.Prepend:
                    sequence.Prepend(tween);
                    break;
                case TweenAdditionType.Insert:
                    sequence.Insert(insertionPosition, tween);
                    break;
                case TweenAdditionType.Join:
                    sequence.Join(tween);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
            return sequence;
        }
    }
}

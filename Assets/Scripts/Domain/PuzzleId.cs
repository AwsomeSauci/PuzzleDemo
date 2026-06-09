using System;

namespace PuzzleFlow.Domain
{
    public readonly struct PuzzleId : IEquatable<PuzzleId>
    {
        public PuzzleId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Puzzle id cannot be empty or whitespace.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool Equals(PuzzleId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PuzzleId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(PuzzleId left, PuzzleId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PuzzleId left, PuzzleId right)
        {
            return !left.Equals(right);
        }
    }
}


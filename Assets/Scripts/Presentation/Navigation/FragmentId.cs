using System;

namespace PuzzleFlow.Presentation.Navigation
{
    public readonly struct FragmentId : IEquatable<FragmentId>
    {
        public FragmentId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public string Value { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public static FragmentId Empty { get; } = new FragmentId(string.Empty);

        public static FragmentId From<TFragment>() where TFragment : Fragment
        {
            return new FragmentId(typeof(TFragment).Name);
        }

        public bool Equals(FragmentId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FragmentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(FragmentId left, FragmentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FragmentId left, FragmentId right)
        {
            return !left.Equals(right);
        }

        public static implicit operator FragmentId(string value)
        {
            return new FragmentId(value);
        }
    }
}


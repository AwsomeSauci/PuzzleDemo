using System;

namespace PuzzleFlow.Domain
{
    [Serializable]
    public struct MediaReference : IEquatable<MediaReference>
    {
        // Public field keeps Unity serialization without a Domain -> UnityEngine dependency.
        public string key;

        public MediaReference(string key)
        {
            this.key = key;
        }

        public string Key => key;
        public bool IsEmpty => string.IsNullOrWhiteSpace(key);

        public bool Equals(MediaReference other)
        {
            return string.Equals(key, other.key, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MediaReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return key == null ? 0 : StringComparer.Ordinal.GetHashCode(key);
        }

        public override string ToString()
        {
            return key ?? string.Empty;
        }
    }
}

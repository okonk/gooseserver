#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Goose
{
    /**
     * A generic Trie (prefix tree) for fast prefix-based lookups.
     *
     * Each node can optionally hold a value of type T. Inserting a key
     * stores the value at the terminal node. LongestPrefixMatch walks
     * the tree following the query characters and returns the value at
     * the deepest node that had one — i.e. the longest registered prefix.
     *
     * Complexity:
     *   Insert:            O(k) where k is the key length
     *   LongestPrefixMatch: O(k) where k is the query length
     *   ContainsKey:       O(k)
     *
     */
    public sealed class Trie<T>
    {
        private readonly TrieNode _root;

        public Trie()
        {
            this._root = new TrieNode();
        }

        /**
         * Insert a key-value pair into the trie.
         * If the key already exists, the value is overwritten.
         */
        public void Insert(string key, T value)
        {
            TrieNode node = this._root;

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];

                if (!node.Children.TryGetValue(c, out TrieNode? child))
                {
                    child = new TrieNode();
                    node.Children[c] = child;
                }

                node = child;
            }

            node.Value = value;
            node.HasValue = true;
        }

        /**
         * Return true if the exact key was previously inserted.
         */
        public bool ContainsKey(string key)
        {
            TrieNode? node = this._Find(key);
            return node is { HasValue: true };
        }

        /**
         * Try to get the value for an exact key match.
         */
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
        {
            TrieNode? node = this._Find(key);
            if (node is { HasValue: true })
            {
                value = node.Value;
                return true;
            }

            value = default;
            return false;
        }

        /**
         * Walk the trie following the query characters and return the value
         * stored at the deepest node encountered — the longest registered
         * prefix of the query.
         *
         * Returns true if at least one prefix was found.
         * The matchedLength output parameter indicates how many characters
         * of the query were consumed by the match.
         */
        public bool TryGetLongestPrefix(string query, [MaybeNullWhen(false)] out T value, out int matchedLength)
        {
            TrieNode node = this._root;
            T bestValue = default!;
            int bestLength = -1;

            for (int i = 0; i < query.Length; i++)
            {
                char c = query[i];

                if (!node.Children.TryGetValue(c, out TrieNode? child))
                {
                    break;
                }

                node = child;

                if (node.HasValue)
                {
                    bestValue = node.Value;
                    bestLength = i + 1;
                }
            }

            value = bestValue;
            matchedLength = bestLength;
            return bestLength >= 0;
        }

        /**
         * Internal: walk the trie for the full key. Returns the terminal
         * node if the path exists, or null if it diverges early.
         */
        private TrieNode? _Find(string key)
        {
            TrieNode node = this._root;

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];

                if (!node.Children.TryGetValue(c, out TrieNode? child))
                {
                    return null;
                }

                node = child;
            }

            return node;
        }

        /**
         * A single node in the trie.
         */
        private sealed class TrieNode
        {
            public Dictionary<char, TrieNode> Children;
            public T Value = default!;
            public bool HasValue;

            public TrieNode()
            {
                this.Children = new Dictionary<char, TrieNode>();
            }
        }
    }
}

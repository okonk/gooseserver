using System;
using System.Collections.Generic;
using System.Linq;

namespace Goose
{
    /**
     * LoginThrottle, rate limits failed login attempts
     *
     * Previously nothing tracked failed logins at all. A wrong password only logged and
     * closed the socket, so an attacker could reconnect and guess again indefinitely at
     * a cost of one TCP handshake per attempt.
     *
     * Failures are counted per key within a sliding window. Once the limit is reached the
     * key is locked out for a period. Both the source IP and the account name are tracked
     * as separate keys, so spraying one password across many accounts is limited by the
     * IP counter, and distributed guessing at one account is limited by the name counter.
     *
     * Successful logins clear both keys.
     *
     * This is deliberately in-process and resets on restart. It exists to make online
     * guessing impractical, not to be an audit trail - LogHandler already records
     * InvalidPassword events for that.
     *
     */
    public class LoginThrottle
    {
        private class Entry
        {
            public int Failures;
            public DateTime WindowStart;
            public DateTime LockedUntil;
        }

        /**
         * Above this many tracked keys we sweep expired entries. Bounds memory under a
         * flood of attempts from many distinct addresses.
         */
        private const int PruneThreshold = 4096;

        private readonly Dictionary<string, Entry> entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        private int FailureLimit
        {
            get { return Math.Max(1, GameWorld.Settings.LoginFailureLimit); }
        }

        private TimeSpan Window
        {
            get { return TimeSpan.FromSeconds(Math.Max(1, GameWorld.Settings.LoginFailureWindowSeconds)); }
        }

        private TimeSpan Lockout
        {
            get { return TimeSpan.FromSeconds(Math.Max(1, GameWorld.Settings.LoginLockoutSeconds)); }
        }

        /**
         * IsLocked, true if this key is currently locked out
         *
         * retryAfter is the remaining lockout, rounded up to whole seconds.
         *
         */
        public bool IsLocked(string key, out int retryAfterSeconds)
        {
            retryAfterSeconds = 0;

            if (string.IsNullOrEmpty(key)) return false;
            if (!this.entries.TryGetValue(key, out Entry entry)) return false;

            DateTime now = DateTime.UtcNow;

            if (entry.LockedUntil <= now) return false;

            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((entry.LockedUntil - now).TotalSeconds));
            return true;
        }

        /**
         * RecordFailure, counts a failed attempt and locks the key if over the limit
         *
         */
        public void RecordFailure(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            DateTime now = DateTime.UtcNow;

            if (!this.entries.TryGetValue(key, out Entry entry))
            {
                entry = new Entry { WindowStart = now };
                this.entries[key] = entry;

                if (this.entries.Count > PruneThreshold) this.Prune(now);
            }

            // Start a fresh window if the previous one has expired.
            if (now - entry.WindowStart > this.Window)
            {
                entry.WindowStart = now;
                entry.Failures = 0;
            }

            entry.Failures++;

            if (entry.Failures >= this.FailureLimit)
            {
                entry.LockedUntil = now + this.Lockout;
                entry.Failures = 0;
                entry.WindowStart = now;
            }
        }

        /**
         * Clear, forgets a key after a successful login
         *
         */
        public void Clear(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            this.entries.Remove(key);
        }

        private void Prune(DateTime now)
        {
            var stale = this.entries
                .Where(e => e.Value.LockedUntil <= now && now - e.Value.WindowStart > this.Window)
                .Select(e => e.Key)
                .ToList();

            foreach (string key in stale)
            {
                this.entries.Remove(key);
            }
        }
    }
}

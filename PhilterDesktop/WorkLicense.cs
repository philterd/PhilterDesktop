/*
 * Copyright 2026 Philterd, LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// The work-use license on this computer: stores a verified token, says whether it is current, and
    /// refreshes it about monthly. Nothing is ever locked; a missing or lapsed license only means the app
    /// keeps reminding that work use needs one.
    /// </summary>
    internal sealed class WorkLicense
    {
        /// <summary>How often a stored token is refreshed while online.</summary>
        internal static readonly TimeSpan RefreshInterval = TimeSpan.FromDays(30);

        private readonly LicenseRepository _repository;
        private readonly IReadOnlyDictionary<string, string> _keys;

        public WorkLicense(LicenseRepository repository) : this(repository, LicenseToken.ProductionKeys)
        {
        }

        /// <summary>With the keys tests can supply.</summary>
        internal WorkLicense(LicenseRepository repository, IReadOnlyDictionary<string, string> keys)
        {
            _repository = repository;
            _keys = keys;
        }

        /// <summary>The public keys tokens are checked against.</summary>
        internal IReadOnlyDictionary<string, string> Keys => _keys;

        /// <summary>The stored license if it verifies, current or not; null if none or it doesn't verify.</summary>
        public LicenseInfo? Stored() => LicenseToken.Verify(_repository.Get()?.Token, _keys);

        /// <summary>The stored license if it verifies and is current (through its expiry plus grace).</summary>
        public LicenseInfo? Current(DateTime nowUtc) => Stored() is { } info && info.IsCurrent(nowUtc) ? info : null;

        /// <summary>Stores <paramref name="token"/> if it verifies. Returns what it carries, or null (nothing stored).</summary>
        public LicenseInfo? Accept(string? token, DateTime nowUtc)
        {
            LicenseInfo? info = LicenseToken.Verify(token, _keys);
            if (info is not null)
            {
                _repository.Save(token!.Trim(), nowUtc);
            }
            return info;
        }

        public void Remove() => _repository.Clear();

        /// <summary>
        /// Due when a token is stored and it's been <see cref="RefreshInterval"/> since the last refresh,
        /// the clock moved back past it, or the license has run out (a renewal may be waiting).
        /// </summary>
        public bool IsRefreshDue(DateTime nowUtc)
        {
            LicenseEntity? stored = _repository.Get();
            if (stored is null || string.IsNullOrEmpty(stored.Token))
            {
                return false;
            }
            DateTime last = stored.LastRefreshUtc;
            bool lapsed = LicenseToken.Verify(stored.Token, _keys) is { } info && nowUtc.Date > info.Expires.Date;
            return lapsed || last > nowUtc || nowUtc - last >= RefreshInterval;
        }

        /// <summary>
        /// Refreshes the stored token if due. A new token that verifies replaces it; a definite "not
        /// licensed" or a refused token removes it; anything else (offline, rate limited, a token that
        /// doesn't verify) keeps it and tries again later.
        /// </summary>
        public async Task<LicenseOutcome?> RefreshIfDueAsync(LicenseClient client, DateTime nowUtc, CancellationToken ct = default)
        {
            if (!IsRefreshDue(nowUtc) || _repository.Get()?.Token is not { Length: > 0 } token)
            {
                return null;
            }
            LicenseResult result = await client.RefreshAsync(token, ct);
            switch (result.Outcome)
            {
                case LicenseOutcome.Licensed:
                    return Accept(result.Token, nowUtc) is not null ? LicenseOutcome.Licensed : LicenseOutcome.Unavailable;
                case LicenseOutcome.NotLicensed:
                case LicenseOutcome.InvalidToken:
                    Remove();
                    return result.Outcome;
                default:
                    return result.Outcome;
            }
        }
    }
}

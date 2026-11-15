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

using System.Globalization;

namespace PhilterDesktop
{
    /// <summary>The result of one step in entering a license, with the message to show.</summary>
    internal sealed record ActivationStep(bool Success, string Message, LicenseInfo? License = null);

    /// <summary>
    /// The steps behind the Manage License dialog, kept out of the form so they can be tested: email a code,
    /// exchange it for a token, import a license file, or remove the license.
    /// </summary>
    internal sealed class LicenseActivation
    {
        internal const string InvalidEmailMessage = "Enter the email address you used to buy your license.";
        internal const string CodeSentMessage =
            "If that email has a purchase, a code is on its way. It can take a minute; check your spam folder too.";
        internal const string NoCodeRequestedMessage = "Send a code to your email first.";
        internal const string EmptyCodeMessage = "Enter the code from the email.";
        internal const string InvalidCodeMessage = "That code didn't work. Check it, or send a new code.";
        internal const string RateLimitedMessage = "Too many attempts. Wait a few minutes and try again.";
        internal const string UnreachableMessage = "Couldn't reach philterd.ai. Check your connection and try again.";
        internal const string BadTokenMessage =
            "The license couldn't be checked. Please contact support@philterd.ai.";
        internal const string InvalidFileMessage = "That file isn't a valid Philter Desktop license.";
        internal const string ExpiredFileMessage =
            "That license has expired. Download a current license file from your account.";
        internal const string RemovedMessage = "The license was removed from this computer.";
        internal const string UnexpectedErrorMessage =
            "Something went wrong saving the license. Please try again, or contact support@philterd.ai.";

        /// <summary>License files are one short line; anything far bigger isn't one.</summary>
        internal const int MaxLicenseFileBytes = 16 * 1024;

        private readonly WorkLicense _license;
        private readonly LicenseClient _client;
        private readonly Func<DateTime> _utcNow;

        public LicenseActivation(WorkLicense license, LicenseClient client, Func<DateTime> utcNow)
        {
            _license = license;
            _client = client;
            _utcNow = utcNow;
        }

        /// <summary>The email a code was last requested for.</summary>
        public string? Email { get; private set; }

        /// <summary>The server's id for that request, sent back with the code. Kept only in memory.</summary>
        public string? RequestId { get; private set; }

        public bool CodeRequested => RequestId is not null;

        /// <summary>A light check that catches typos; the server is the judge.</summary>
        internal static bool LooksLikeEmail(string? email)
        {
            email = email?.Trim();
            if (string.IsNullOrEmpty(email) || email.Length > 254 || email.Any(char.IsWhiteSpace))
            {
                return false;
            }
            int at = email.IndexOf('@');
            return at > 0 && at == email.LastIndexOf('@') && email.IndexOf('.', at) > at + 1 && !email.EndsWith('.');
        }

        public async Task<ActivationStep> SendCodeAsync(string email)
        {
            if (!LooksLikeEmail(email))
            {
                return new ActivationStep(false, InvalidEmailMessage);
            }
            string? request = await _client.RequestCodeAsync(email);
            if (request is null)
            {
                return new ActivationStep(false, UnreachableMessage);
            }
            Email = LicenseClient.NormalizeEmail(email);
            RequestId = request;
            return new ActivationStep(true, CodeSentMessage);
        }

        public async Task<ActivationStep> VerifyAsync(string code)
        {
            if (Email is null || RequestId is null)
            {
                return new ActivationStep(false, NoCodeRequestedMessage);
            }
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ActivationStep(false, EmptyCodeMessage);
            }
            LicenseResult result = await _client.ExchangeCodeAsync(Email, code, RequestId);
            switch (result.Outcome)
            {
                case LicenseOutcome.Licensed:
                    LicenseInfo? info = _license.Accept(result.Token, _utcNow());
                    RequestId = null; // the code is used up
                    return info is null
                        ? new ActivationStep(false, BadTokenMessage)
                        : new ActivationStep(true, LicensedMessage(info), info);
                case LicenseOutcome.NotLicensed:
                    RequestId = null;
                    return new ActivationStep(false, NotLicensedMessage(Email));
                case LicenseOutcome.InvalidCode:
                    return new ActivationStep(false, InvalidCodeMessage);
                case LicenseOutcome.RateLimited:
                    return new ActivationStep(false, RateLimitedMessage);
                default:
                    // The code isn't used up after a failure to answer, so the same code can be retried.
                    return new ActivationStep(false, UnreachableMessage);
            }
        }

        /// <summary>Imports a license file's text (for a computer that's never online).</summary>
        public ActivationStep Import(string fileText)
        {
            DateTime now = _utcNow();
            if (LicenseToken.Verify(fileText, KeysForImport()) is not { } info)
            {
                return new ActivationStep(false, InvalidFileMessage);
            }
            if (!info.IsCurrent(now))
            {
                return new ActivationStep(false, ExpiredFileMessage);
            }
            LicenseInfo? stored = _license.Accept(fileText, now);
            return stored is null
                ? new ActivationStep(false, InvalidFileMessage)
                : new ActivationStep(true, LicensedMessage(stored), stored);
        }

        public ActivationStep Remove()
        {
            _license.Remove();
            return new ActivationStep(true, RemovedMessage);
        }

        // Import checks against the same keys the stored license uses.
        private IReadOnlyDictionary<string, string> KeysForImport() => _license.Keys;

        internal static string LicensedMessage(LicenseInfo info) =>
            $"Verified. {Describe(info)}";

        internal static string NotLicensedMessage(string email) =>
            $"There's no current license for {email}. If you bought one with a different email, use that address.";

        /// <summary>"Licensed for work use to jane@example.com (3 seats) through November 15, 2027."</summary>
        internal static string Describe(LicenseInfo info)
        {
            string seats = info.Seats > 1 ? $" ({info.Seats} seats)" : string.Empty;
            string through = info.Expires.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture);
            return $"Licensed for work use to {info.Email}{seats} through {through}.";
        }
    }
}

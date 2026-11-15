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

using System.Net;
using System.Net.Http;
using LiteDB;
using PhilterData;
using Xunit;
using Stub = PhilterDesktop.Tests.TestLicenses.StubHandler;

namespace PhilterDesktop.Tests
{
    /// <summary>Storing, checking, and refreshing the work license (against a real temporary database).</summary>
    public sealed class WorkLicenseTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        private readonly string _path = Path.Combine(Path.GetTempPath(), "license-" + Guid.NewGuid().ToString("N") + ".db");
        private readonly LiteDatabase _db;
        private readonly LicenseRepository _repo;
        private readonly WorkLicense _license;

        public WorkLicenseTests()
        {
            _db = new LiteDatabase(_path);
            _repo = new LicenseRepository(_db);
            _license = new WorkLicense(_repo, TestLicenses.Keys);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_path); } catch { /* best effort */ }
        }

        private static LicenseClient Answering(string json, HttpStatusCode status = HttpStatusCode.OK) =>
            TestLicenses.Client(Stub.Json(status, json));

        // --- Nothing stored ----------------------------------------------------------------------

        [Fact]
        public async Task NoLicense_IsNotCurrent_AndNeverCallsTheServer()
        {
            Assert.Null(_license.Stored());
            Assert.Null(_license.Current(Now));
            Assert.False(_license.IsRefreshDue(Now));
            Stub stub = Stub.Throws(new InvalidOperationException("must not be called"));
            Assert.Null(await _license.RefreshIfDueAsync(TestLicenses.Client(stub), Now));
            Assert.Empty(stub.Requests);
        }

        // --- Accepting a token -------------------------------------------------------------------

        [Fact]
        public void Accept_StoresAVerifiedToken()
        {
            LicenseInfo? info = _license.Accept(TestLicenses.Valid + "\n", Now);
            Assert.Equal("jane@example.com", info?.Email);
            Assert.Equal(TestLicenses.Valid, _repo.Get()!.Token); // stored trimmed
            Assert.Equal(Now, _repo.Get()!.LastRefreshUtc);
            Assert.NotNull(_license.Current(Now));
        }

        [Theory]
        [InlineData(TestLicenses.TamperedPayload)]
        [InlineData(TestLicenses.BadSignature)]
        [InlineData("garbage")]
        [InlineData("")]
        public void Accept_RefusesABadToken_AndStoresNothing(string token)
        {
            Assert.Null(_license.Accept(token, Now));
            Assert.Null(_repo.Get());
        }

        [Fact]
        public void Accept_ABadToken_KeepsTheExistingLicense()
        {
            _license.Accept(TestLicenses.Valid, Now);
            Assert.Null(_license.Accept(TestLicenses.BadSignature, Now));
            Assert.Equal(TestLicenses.Valid, _repo.Get()!.Token);
        }

        // A license signed with a key this build doesn't trust is stored nowhere and counts for nothing.
        [Fact]
        public void ProductionBuild_RefusesTestKeyTokens()
        {
            var production = new WorkLicense(_repo);
            Assert.Null(production.Accept(TestLicenses.Valid, Now));
            Assert.Null(_repo.Get());
        }

        [Fact]
        public void Current_EndsAfterTheGracePeriod_ButTheLicenseStaysStored()
        {
            _license.Accept(TestLicenses.Valid, Now); // expires 2027-11-15
            Assert.NotNull(_license.Current(new DateTime(2027, 12, 15, 0, 0, 0, DateTimeKind.Utc)));
            Assert.Null(_license.Current(new DateTime(2027, 12, 16, 0, 0, 0, DateTimeKind.Utc)));
            Assert.NotNull(_license.Stored()); // kept so a renewal can still refresh it
        }

        [Fact]
        public void Remove_ClearsTheLicense()
        {
            _license.Accept(TestLicenses.Valid, Now);
            _license.Remove();
            Assert.Null(_license.Stored());
            Assert.Null(_repo.Get());
        }

        [Fact]
        public void License_PersistsAcrossInstances()
        {
            _license.Accept(TestLicenses.Valid, Now);
            var again = new WorkLicense(new LicenseRepository(_db), TestLicenses.Keys);
            Assert.Equal("jane@example.com", again.Current(Now)?.Email);
        }

        // Kept apart from the settings record, so saving settings can't overwrite a refreshed license.
        [Fact]
        public void License_IsNotPartOfTheSettingsRecord()
        {
            _license.Accept(TestLicenses.Valid, Now);
            var settings = new SettingsRepository(_db);
            settings.SaveSettings(settings.GetSettings());
            Assert.True(_db.CollectionExists("license"));
            Assert.Equal(TestLicenses.Valid, _repo.Get()!.Token);
        }

        // --- When a refresh is due ------------------------------------------------------------------

        [Theory]
        [InlineData(0, false)]
        [InlineData(29, false)]
        [InlineData(30, true)]
        [InlineData(90, true)]
        public void Refresh_IsMonthly(int daysLater, bool expected)
        {
            _license.Accept(TestLicenses.Valid, Now);
            Assert.Equal(expected, _license.IsRefreshDue(Now.AddDays(daysLater)));
        }

        [Fact]
        public void Refresh_IsDue_WhenTheClockMovedBack()
        {
            _license.Accept(TestLicenses.Valid, Now);
            Assert.True(_license.IsRefreshDue(Now.AddDays(-1)));
        }

        // Past its expiry date a license may have renewed, so it's checked without waiting a month.
        [Fact]
        public void Refresh_IsDue_OnceTheLicenseHasExpired()
        {
            DateTime justBefore = new(2027, 11, 14, 0, 0, 0, DateTimeKind.Utc);
            _license.Accept(TestLicenses.Valid, justBefore);
            Assert.False(_license.IsRefreshDue(justBefore.AddDays(1)));       // the expiry date itself
            Assert.True(_license.IsRefreshDue(justBefore.AddDays(2)));        // the day after
        }

        // --- Refresh outcomes ----------------------------------------------------------------------

        [Fact]
        public async Task Refresh_ReplacesTheTokenWithTheNewOne()
        {
            _license.Accept(TestLicenses.Valid, Now);
            string renewed = TestLicenses.Make(expires: "2028-11-15");
            DateTime later = Now.AddDays(31);

            Assert.Equal(LicenseOutcome.Licensed, await _license.RefreshIfDueAsync(Answering($"{{\"licensed\":true,\"token\":\"{renewed}\"}}"), later));
            Assert.Equal(new DateTime(2028, 11, 15, 0, 0, 0, DateTimeKind.Utc), _license.Stored()!.Expires);
            Assert.Equal(later, _repo.Get()!.LastRefreshUtc);
            Assert.False(_license.IsRefreshDue(later));
        }

        // A changed purchaser email from the server is taken as given (the server decides).
        [Fact]
        public async Task Refresh_AdoptsTheTokensEmail()
        {
            _license.Accept(TestLicenses.Valid, Now);
            string moved = TestLicenses.Make(email: "new.owner@example.com");
            await _license.RefreshIfDueAsync(Answering($"{{\"licensed\":true,\"token\":\"{moved}\"}}"), Now.AddDays(31));
            Assert.Equal("new.owner@example.com", _license.Stored()!.Email);
        }

        [Fact]
        public async Task Refresh_KeepsTheOldToken_WhenTheNewOneDoesntVerify()
        {
            _license.Accept(TestLicenses.Valid, Now);
            DateTime later = Now.AddDays(31);
            Assert.Equal(LicenseOutcome.Unavailable,
                await _license.RefreshIfDueAsync(Answering($"{{\"licensed\":true,\"token\":\"{TestLicenses.TamperedPayload}\"}}"), later));
            Assert.Equal(TestLicenses.Valid, _repo.Get()!.Token);
            Assert.True(_license.IsRefreshDue(later)); // tried again later
        }

        [Theory]
        [InlineData("{\"licensed\":false}", HttpStatusCode.OK, LicenseOutcome.NotLicensed)]
        [InlineData("{\"error\":\"invalid_token\"}", HttpStatusCode.BadRequest, LicenseOutcome.InvalidToken)]
        public async Task Refresh_RemovesTheLicense_OnADefiniteNo(string json, HttpStatusCode status, LicenseOutcome expected)
        {
            _license.Accept(TestLicenses.Valid, Now);
            Assert.Equal(expected, await _license.RefreshIfDueAsync(Answering(json, status), Now.AddDays(31)));
            Assert.Null(_license.Stored());
        }

        [Theory]
        [InlineData("{\"error\":\"unavailable\"}", HttpStatusCode.ServiceUnavailable, LicenseOutcome.Unavailable)]
        [InlineData("{\"error\":\"rate_limited\"}", HttpStatusCode.TooManyRequests, LicenseOutcome.RateLimited)]
        [InlineData("<html>portal</html>", HttpStatusCode.OK, LicenseOutcome.Unavailable)]
        public async Task Refresh_KeepsTheLicense_WhenThereIsNoAnswer(string json, HttpStatusCode status, LicenseOutcome expected)
        {
            _license.Accept(TestLicenses.Valid, Now);
            DateTime later = Now.AddDays(31);
            Assert.Equal(expected, await _license.RefreshIfDueAsync(Answering(json, status), later));
            Assert.Equal(TestLicenses.Valid, _repo.Get()!.Token);
            Assert.Equal(Now, _repo.Get()!.LastRefreshUtc); // not counted as a refresh
        }

        [Fact]
        public async Task Refresh_Offline_KeepsTheLicense()
        {
            _license.Accept(TestLicenses.Valid, Now);
            Assert.Equal(LicenseOutcome.Unavailable,
                await _license.RefreshIfDueAsync(TestLicenses.Client(Stub.Throws(new HttpRequestException("offline"))), Now.AddDays(31)));
            Assert.NotNull(_license.Stored());
        }

        [Fact]
        public async Task Refresh_NotDue_SendsNothing()
        {
            _license.Accept(TestLicenses.Valid, Now);
            Stub stub = Stub.Json(HttpStatusCode.OK, "{\"licensed\":false}");
            Assert.Null(await _license.RefreshIfDueAsync(TestLicenses.Client(stub), Now.AddDays(5)));
            Assert.Empty(stub.Requests);
            Assert.NotNull(_license.Stored());
        }

        [Fact]
        public void RefreshInterval_IsThirtyDays() => Assert.Equal(TimeSpan.FromDays(30), WorkLicense.RefreshInterval);
    }
}

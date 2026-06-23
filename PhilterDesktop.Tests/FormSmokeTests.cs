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

using System.Runtime.ExceptionServices;
using LiteDB;
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using PhilterData;
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Smoke tests that construct each WinForms form (on an STA thread) and dispose it,
    /// catching exceptions in constructors / layout / theming. They don't show the forms
    /// or drive interaction — just guard against initialization regressions.
    /// </summary>
    public sealed class FormSmokeTests
    {
        private static void Sta(Action action)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        private static void ConstructWithDb(Func<LiteDatabase, Form> create)
        {
            Sta(() =>
            {
                string path = Path.Combine(Path.GetTempPath(), "smoke-" + Guid.NewGuid().ToString("N") + ".db");
                try
                {
                    using (var db = new LiteDatabase(path))
                    using (Form form = create(db))
                    {
                        _ = form.Handle; // force handle creation (exercises theming/interop)
                    }
                }
                finally
                {
                    try { File.Delete(path); } catch { /* best effort */ }
                }
            });
        }

        [Fact]
        public void PolicyEditorForm_Constructs() =>
            ConstructWithDb(db => new PolicyEditorForm(new PolicyRepository(db)));

        [Fact]
        public void RedactDocuments_Constructs() =>
            ConstructWithDb(db => new RedactDocuments(
                new PolicyRepository(db), new ContextRepository(db), new RedactionQueueRepository(db), loggingEnabled: false));

        [Fact]
        public void Contexts_Constructs() =>
            ConstructWithDb(db => new Contexts(new ContextRepository(db), new ContextEntryRepository(db)));

        [Fact]
        public void SettingsForm_Constructs() =>
            ConstructWithDb(db => new SettingsForm(new SettingsRepository(db)));

        [Fact]
        public void AboutForm_Constructs() => Sta(() => { using var f = new AboutForm(); _ = f.Handle; });

        [Fact]
        public void LicenseForm_Constructs() => Sta(() => { using var f = new LicenseForm(); _ = f.Handle; });

        [Fact]
        public void CreateContextDialog_Constructs() => Sta(() => { using var f = new CreateContextDialog(); _ = f.Handle; });

        [Fact]
        public void FilterStrategiesForm_Constructs() => Sta(() =>
        {
            using var f = new FilterStrategiesForm("SSN", Array.Empty<object>(), typeof(SsnFilterStrategy));
            _ = f.Handle;
        });

        [Fact]
        public void AddFilterStrategyForm_Constructs() => Sta(() =>
        {
            using var f = new AddFilterStrategyForm(new SsnFilterStrategy(), "SSN");
            _ = f.Handle;
        });

        [Fact]
        public void CustomIdentifiersForm_Constructs() => Sta(() =>
        {
            using var f = new CustomIdentifiersForm(new List<Identifier>());
            _ = f.Handle;
        });
    }
}

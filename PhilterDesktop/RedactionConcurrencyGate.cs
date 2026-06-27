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

using System.Threading;

namespace PhilterDesktop
{
    /// <summary>
    /// Limits how many redactions run at once. Up to <c>maxConcurrency</c> ordinary files run in
    /// parallel; a file requested as <c>solo</c> (e.g. a very large one) runs <b>alone</b> — it waits
    /// for all in-flight redactions to finish and holds the whole gate while it runs, so a big,
    /// memory-heavy file never piles on top of others.
    ///
    /// Acquire a slot with <see cref="EnterAsync"/> and release it by disposing the returned token
    /// (use <c>using</c>). Solo acquisitions are serialized against each other so two big files can't
    /// deadlock by each grabbing part of the gate.
    /// </summary>
    internal sealed class RedactionConcurrencyGate : IDisposable
    {
        private readonly int _maxSlots;
        private readonly SemaphoreSlim _slots;
        private readonly SemaphoreSlim _soloTurn = new(1, 1);

        public RedactionConcurrencyGate(int maxConcurrency)
        {
            _maxSlots = Math.Clamp(maxConcurrency, 1, 16);
            _slots = new SemaphoreSlim(_maxSlots, _maxSlots);
        }

        /// <summary>The configured maximum number of concurrent (non-solo) redactions.</summary>
        public int MaxConcurrency => _maxSlots;

        /// <summary>
        /// Acquires a slot, awaiting until one is available. When <paramref name="solo"/> is true (or
        /// the gate only allows one at a time anyway) the caller runs exclusively. Dispose the result
        /// to release.
        /// </summary>
        public async Task<IDisposable> EnterAsync(bool solo, CancellationToken cancellationToken = default)
        {
            if (!solo && _maxSlots > 1)
            {
                await _slots.WaitAsync(cancellationToken).ConfigureAwait(false);
                return new Releaser(this, slots: 1, releaseSoloTurn: false);
            }

            // Exclusive: take a "solo turn" first (so two solo files don't each grab part of the gate
            // and deadlock), then drain every slot.
            await _soloTurn.WaitAsync(cancellationToken).ConfigureAwait(false);
            int acquired = 0;
            try
            {
                for (; acquired < _maxSlots; acquired++)
                {
                    await _slots.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                if (acquired > 0)
                {
                    _slots.Release(acquired);
                }
                _soloTurn.Release();
                throw;
            }
            return new Releaser(this, slots: _maxSlots, releaseSoloTurn: true);
        }

        public void Dispose()
        {
            _slots.Dispose();
            _soloTurn.Dispose();
        }

        private sealed class Releaser : IDisposable
        {
            private readonly RedactionConcurrencyGate _gate;
            private readonly int _slots;
            private readonly bool _releaseSoloTurn;
            private bool _released;

            public Releaser(RedactionConcurrencyGate gate, int slots, bool releaseSoloTurn)
            {
                _gate = gate;
                _slots = slots;
                _releaseSoloTurn = releaseSoloTurn;
            }

            public void Dispose()
            {
                if (_released)
                {
                    return;
                }
                _released = true;
                _gate._slots.Release(_slots);
                if (_releaseSoloTurn)
                {
                    _gate._soloTurn.Release();
                }
            }
        }
    }
}

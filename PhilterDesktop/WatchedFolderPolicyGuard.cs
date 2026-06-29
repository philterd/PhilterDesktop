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
    /// Helpers that keep watched folders from being orphaned when a policy is deleted: a folder that
    /// references a now-missing policy silently fails every file ("unknown policy"). The policy editor
    /// uses these to warn and reassign affected folders to the default policy (#491).
    /// </summary>
    internal static class WatchedFolderPolicyGuard
    {
        public const string DefaultPolicyName = "default";

        /// <summary>The watched folders that reference <paramref name="policyName"/> (case-insensitive).</summary>
        public static List<WatchedFolderEntity> FoldersUsing(WatchedFolderRepository repository, string policyName) =>
            repository.GetAll()
                .Where(f => string.Equals(f.Policy, policyName, StringComparison.OrdinalIgnoreCase))
                .ToList();

        /// <summary>Switches each folder to the default policy and persists it.</summary>
        public static void ReassignToDefault(WatchedFolderRepository repository, IEnumerable<WatchedFolderEntity> folders)
        {
            foreach (WatchedFolderEntity folder in folders)
            {
                folder.Policy = DefaultPolicyName;
                repository.Update(folder);
            }
        }
    }
}

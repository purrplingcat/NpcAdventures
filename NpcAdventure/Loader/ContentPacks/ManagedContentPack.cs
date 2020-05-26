﻿using System.Collections.Generic;
using NpcAdventure.Loader.ContentPacks.Data;
using NpcAdventure.Loader.ContentPacks.Provider;
using NpcAdventure.Utils;
using StardewModdingAPI;

namespace NpcAdventure.Loader.ContentPacks
{
    /// <summary>Handles loading assets from content packs.</summary>
    internal class ManagedContentPack
    {
        public static string[] SUPPORTED_FORMATS = { "1.1", "1.2", "1.3" };

        /// <summary>The managed content pack.</summary>
        public IContentPack Pack { get; }
        public IMonitor Monitor { get; }

        private readonly LegacyDataProvider legacyDataProvider;

        public Contents Contents { get; private set; }
        public ISemanticVersion FormatVersion { get; internal set; }

        /// <summary>Construct an instance.</summary>
        /// <param name="pack">The content pack to manage.</param>
        public ManagedContentPack(IContentPack pack, IMonitor monitor, bool paranoid = false)
        {
            this.Pack = pack;
            this.Monitor = monitor;
            this.legacyDataProvider = new LegacyDataProvider(this, paranoid);
        }

        public void Load()
        {
            this.Monitor.Log($"   Loading content pack `{this.Pack.Manifest.Name}`");

            if (!this.Pack.HasFile("content.json"))
                throw new ContentPackException("Declaration file `content.json` not found!");

            this.Contents = this.Pack.ReadJsonFile<Contents>("content.json");
            this.FormatVersion = new SemanticVersion(this.Contents.Format);
            this.VerifyContentPack();
        }

        private void VerifyContentPack()
        {
            if (!this.CheckFormatVersion(this.FormatVersion))
                throw new ContentPackException($"Unsupported format `{this.Contents.Format}`");

            this.Monitor.Log($"      Detected format version {this.FormatVersion}");

            if (this.Contents.AllowUnsafePatches)
            {
                this.Monitor.Log($"      Unsafe patches for this content pack are allowed!");
            }

            if (this.FormatVersion.IsOlderThan("1.3"))
            {
                this.Contents.AllowUnsafePatches = true;
                this.Monitor.Log("      Force allow unsafe patches old content pack format (format version <1.3)");
            }

            this.VerifyPatches(this.FormatVersion);
        }

        private void VerifyPatches(ISemanticVersion formatVersion)
        {
            int num = 0; // For identify patches without log name
            for (int i = 0; i < this.Contents.Changes.Count; i++)
            {
                var change = this.Contents.Changes[i];
                var rewriteNotices = this.ApplyPatchRewrites(change, formatVersion);
                var errors = this.ValidatePatchDefinition(change);

                if (change.LogName == null)
                    change.LogName = $"Patch #{num}";

                if (change.Action == "Replace")
                {
                    this.Monitor.Log($"      Detected content replacer `{change.LogName}` for `{change.Target}`");
                }

                if (change.CanOverride)
                {
                    this.Monitor.Log($"      Existing key overrides by `{change.LogName}` allowed for `{change.Target}`");
                }

                if (rewriteNotices.Count > 0)
                {
                    rewriteNotices.ForEach(e => this.Monitor.Log($"      {e} in patch `{change.LogName}`"));
                }

                if (errors.Count > 0)
                {
                    this.Monitor.Log($"Skipped content pack `{this.Pack.Manifest.Name}` patch `{change.LogName}` due to errors:", LogLevel.Error);
                    errors.ForEach(e => this.Monitor.Log($"   - {e}", LogLevel.Error));
                    this.Contents.Changes.RemoveAt(i--);
                }

                num++;
            }
        }

        private List<string> ApplyPatchRewrites(LegacyChanges change, ISemanticVersion formatVersion)
        {
            List<string> notices = new List<string>();

            if (formatVersion.IsOlderThan("1.2") && !string.IsNullOrEmpty(change.Locale))
            {
                // Locales exists in format version 1.2 and newer. For older formats is locale undefined
                change.Locale = null;
                notices.Add($"Ignore field `Locale` in format version `{formatVersion}`");
            }

            if (formatVersion.IsOlderThan("1.2") && !string.IsNullOrEmpty(change.LogName))
            {
                // Locales exists in format version 1.2 and newer. For older formats is locale undefined
                change.LogName = null;
                notices.Add($"Ignore field `LogName` in format version `{formatVersion}`");
            }

            if (!formatVersion.IsOlderThan("1.3") && string.IsNullOrEmpty(change.Action))
            {
                change.Action = "Patch"; // Action patch is a default action in format >=1.3
            }

            if (formatVersion.IsOlderThan("1.3") && (change.Action == "Load" || change.Action == "Edit"))
            {
                var replace = change.Action == "Load" ? "Replace" : "Patch";

                notices.Add($"Rewrite action `{change.Action}` -> `{replace}`");
                change.Action = replace;
                change.CanOverride = true;
            }

            return notices;
        }

        private List<string> ValidatePatchDefinition(LegacyChanges change)
        {
            List<string> problems = new List<string>();

            if (string.IsNullOrEmpty(change.Action))
                problems.Add($"Undefined action in patch");
            if (string.IsNullOrEmpty(change.Target))
                problems.Add($"Target is not defined in entry");
            if (string.IsNullOrEmpty(change.FromFile))
                problems.Add("No content defined! `FromFile` must be set in entry");
            if (change.Action != null && change.Action.Equals("Replace") && !string.IsNullOrEmpty(change.Locale))
                problems.Add("Locale can't be used for `Replace` action! Use action `Patch` instead for localization patches");
            if (change.Action != null && !change.Action.Equals("Replace") && !change.Action.Equals("Patch"))
                problems.Add($"Unknown action `{change.Action}`");
            if (change.Action == "Replace" && !this.Contents.AllowUnsafePatches)
                problems.Add($"Can't use action `Replace` in safe mode! Set `AllowUnsafePatches` to `true` or remove this patch.");
            if (change.CanOverride && !this.Contents.AllowUnsafePatches)
                problems.Add($"Can't allow key overrides by this patch in safe mode! Set `AllowUnsafePatches` to `true` or remove this patch.");

            return problems;
        }

        private bool CheckFormatVersion(ISemanticVersion semanticVersion)
        {
            foreach (var compareTo in SUPPORTED_FORMATS)
            {
                if (semanticVersion.EqualsMajorMinor(new SemanticVersion(compareTo)))
                    return true;
            }

            return false;
        }

        public bool Apply<TKey, TValue>(Dictionary<TKey, TValue> target, string path)
        {
            return this.legacyDataProvider.Apply(target, path);
        }
    }
}
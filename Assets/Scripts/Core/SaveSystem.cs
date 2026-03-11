using System;
using System.Collections.Generic;
using System.IO;
using MatchingCards.Config;
using MatchingCards.Model;
using UnityEngine;

namespace MatchingCards.Core
{
    /// <summary>
    /// Handles serialization of MatchingCardsModel to and from persistent storage.
    ///
    /// Unity's JsonUtility is used for serialization. Because UnityEngine.Object
    /// references (e.g. Sprite) cannot survive JSON round-trips, CardMeta references
    /// are stripped on save and re-linked on load using PairId as a stable key
    /// into the GameConfig card list.
    /// </summary>
    public static class SaveSystem
    {
        const string SaveFileName = "MatchingCards.json";

        static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        /// <summary>True when a save file exists on disk.</summary>
        public static bool HasSave => File.Exists(SavePath);

        // ── Save ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Serializes <paramref name="model"/> to JSON and writes it to persistent storage.
        /// Any UnityEngine.Object references inside the model (e.g. Sprites) are silently
        /// omitted by JsonUtility and will be restored on the next <see cref="TryLoad"/>.
        /// </summary>
        public static void Save(MatchingCardsModel model)
        {
            try
            {
                string json = JsonUtility.ToJson(model, prettyPrint: false);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }

        // ── Load ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to deserialize a model from disk.
        /// <para>
        /// After deserialization, Sprite references in each <see cref="CardData"/> are
        /// re-linked from <paramref name="availableMetas"/> using
        /// <c>PairId % availableMetas.Count</c> — the same mapping used in
        /// <see cref="MatchingCardsModel.BuildCards"/>.
        /// </para>
        /// </summary>
        /// <param name="availableMetas">
        /// The card meta list from <see cref="GameConfig"/>. Must not be null or empty.
        /// </param>
        /// <param name="model">The deserialized model, or null on failure.</param>
        /// <returns>True when a valid save was found and loaded successfully.</returns>
        public static bool TryLoad(List<CardMeta> availableMetas, out MatchingCardsModel model)
        {
            model = null;

            if (!HasSave) return false;

            try
            {
                string json = File.ReadAllText(SavePath);
                model = JsonUtility.FromJson<MatchingCardsModel>(json);
                RelinkCardMetas(model, availableMetas);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                return false;
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────

        /// <summary>
        /// Deletes the save file from disk. Safe to call when no save exists.
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (HasSave) File.Delete(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Delete failed: {e.Message}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Re-links <see cref="CardMeta"/> references (including Sprites) for every card
        /// after JSON deserialization. PairId is used as a stable lookup key.
        /// </summary>
        static void RelinkCardMetas(MatchingCardsModel model, List<CardMeta> availableMetas)
        {
            if (model?.Cards == null || availableMetas == null || availableMetas.Count == 0)
            {
                Debug.LogWarning("[SaveSystem] Cannot relink card metas: model or meta list is null/empty.");
                return;
            }

            foreach (var card in model.Cards)
                card.Meta = availableMetas[card.PairId % availableMetas.Count];
        }
    }
}

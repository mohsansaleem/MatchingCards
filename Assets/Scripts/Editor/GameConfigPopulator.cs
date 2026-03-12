using System.IO;
using MatchingCards.Config;
using UnityEditor;
using UnityEngine;
using GridLayout = MatchingCards.Config.GridLayout;

namespace MatchingCards.Editor
{
    /// <summary>
    /// Editor utility that scans the project's asset folders and auto-populates
    /// a GameConfig ScriptableObject with sprites, audio clips, card back image,
    /// and the standard set of grid layouts.
    ///
    /// Usage: select a GameConfig asset, then run
    ///   Tools → MatchingCards → Populate GameConfig from Assets
    /// </summary>
    public static class GameConfigPopulator
    {
        const string FacesPath   = "Assets/Environment/Sprites/Cards/Faces";
        const string BackPath    = "Assets/Environment/Sprites/Cards/Back/card_back.png";
        const string AudioPath   = "Assets/Environment/Audio";
        const string FlipName    = "flip";
        const string MatchName   = "match";
        const string MismatchName= "mismatch";
        const string GameOverName= "gameover";

        [MenuItem("Tools/MatchingCards/Populate GameConfig from Assets")]
        static void Populate()
        {
            // ── Find selected GameConfig ──────────────────────────────────────
            var config = Selection.activeObject as GameConfig;
            if (config == null)
            {
                EditorUtility.DisplayDialog(
                    "No GameConfig selected",
                    "Please select a GameConfig ScriptableObject asset in the Project window, then run this menu item.",
                    "OK");
                return;
            }

            Undo.RecordObject(config, "Populate GameConfig from Assets");

            int  cardCount  = PopulateCardFaces(config);
            bool backOk     = PopulateCardBack(config);
            int  audioCount = PopulateAudio(config);
            int  layoutCount= PopulateGridLayouts(config);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "GameConfig Populated",
                $"Done!\n\n" +
                $"• Card faces   : {cardCount}\n" +
                $"• Card back    : {(backOk ? "✓" : "not found")}\n" +
                $"• Audio clips  : {audioCount}/4\n" +
                $"• Grid layouts : {layoutCount}",
                "OK");
        }

        // ── Card faces ────────────────────────────────────────────────────────

        static int PopulateCardFaces(GameConfig config)
        {
            if (!Directory.Exists(FacesPath))
            {
                Debug.LogWarning($"[GameConfigPopulator] Faces folder not found: {FacesPath}");
                return 0;
            }

            config.CardMetaOptions.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { FacesPath });
            foreach (string guid in guids)
            {
                string path   = AssetDatabase.GUIDToAssetPath(guid);
                var    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                string cardName = Path.GetFileNameWithoutExtension(path);
                cardName = char.ToUpper(cardName[0]) + cardName.Substring(1); // capitalise

                config.CardMetaOptions.Add(new CardMeta
                {
                    Name   = cardName,
                    Sprite = sprite
                });
            }

            Debug.Log($"[GameConfigPopulator] Added {config.CardMetaOptions.Count} card faces.");
            return config.CardMetaOptions.Count;
        }

        // ── Card back ─────────────────────────────────────────────────────────

        static bool PopulateCardBack(GameConfig config)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[GameConfigPopulator] Card back not found at: {BackPath}");
                return false;
            }

            config.CardBackSprite = sprite;
            Debug.Log("[GameConfigPopulator] Card back sprite assigned.");
            return true;
        }

        // ── Audio ─────────────────────────────────────────────────────────────

        static int PopulateAudio(GameConfig config)
        {
            int count = 0;

            config.FlipClip     = LoadClip(FlipName,     ref count);
            config.MatchClip    = LoadClip(MatchName,    ref count);
            config.MismatchClip = LoadClip(MismatchName, ref count);
            config.GameOverClip = LoadClip(GameOverName, ref count);

            Debug.Log($"[GameConfigPopulator] Assigned {count}/4 audio clips.");
            return count;
        }

        static AudioClip LoadClip(string baseName, ref int count)
        {
            // Search for any audio asset whose filename starts with baseName
            string[] guids = AssetDatabase.FindAssets($"{baseName} t:AudioClip", new[] { AudioPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (!file.Equals(baseName, System.StringComparison.OrdinalIgnoreCase)) continue;

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) { count++; return clip; }
            }

            Debug.LogWarning($"[GameConfigPopulator] Audio clip not found: {baseName}");
            return null;
        }

        // ── Grid layouts ──────────────────────────────────────────────────────

        /// <summary>
        /// Replaces the layout list with the standard set used by the game.
        /// Only even-total layouts are included (required for pairing).
        /// </summary>
        static int PopulateGridLayouts(GameConfig config)
        {
            config.GridLayouts.Clear();
            config.GridLayouts.AddRange(new[]
            {
                new GridLayout { Label = "2×2",  Rows = 2, Columns = 2  },
                new GridLayout { Label = "2×3",  Rows = 2, Columns = 3  },
                new GridLayout { Label = "3×4",  Rows = 3, Columns = 4  },
                new GridLayout { Label = "4×4",  Rows = 4, Columns = 4  },
                new GridLayout { Label = "4×5",  Rows = 4, Columns = 5  },
                new GridLayout { Label = "5×6",  Rows = 5, Columns = 6  },
            });

            Debug.Log($"[GameConfigPopulator] Added {config.GridLayouts.Count} grid layouts.");
            return config.GridLayouts.Count;
        }
    }
}

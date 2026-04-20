using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerAttackSetupHelper : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlayerAttacks playerAttacks;

    [Header("Source Sprite Sheet")]
    [Tooltip("Assign the sliced sprite sheet asset (for example left_jab.png).")]
    [SerializeField] private Sprite sourceSpriteSheet;

    [Header("Filter")]
    [Tooltip("Only sprites with this prefix are used, for example left_jab_.")]
    [SerializeField] private string framePrefix = "left_jab_";

    [Tooltip("Which attack entry to fill.")]
    [SerializeField] private AttackType targetAttackType = AttackType.Jab;

    [ContextMenu("Auto Fill Attack Frames")]
    public void AutoFillAttackFrames()
    {
        if (playerAttacks == null)
        {
            playerAttacks = GetComponent<PlayerAttacks>();
        }

        if (playerAttacks == null)
        {
            Debug.LogError("PlayerAttackSetupHelper: No PlayerAttacks component found.");
            return;
        }

        if (sourceSpriteSheet == null)
        {
            Debug.LogError("PlayerAttackSetupHelper: Assign a sliced sprite sheet to Source Sprite Sheet.");
            return;
        }

        if (string.IsNullOrWhiteSpace(framePrefix))
        {
            Debug.LogError("PlayerAttackSetupHelper: Frame Prefix cannot be empty.");
            return;
        }

        AttackData attackData = FindOrCreateAttackData(playerAttacks, targetAttackType);
        if (attackData == null)
        {
            Debug.LogError($"PlayerAttackSetupHelper: Could not create or find attack type {targetAttackType}.");
            return;
        }

        // In normal editor usage, Unity serializes sliced sprites as sub-assets,
        // and FindObjectsOfTypeAll can discover them by shared texture.
        Sprite[] allSprites = TryCollectSubSpritesFromSceneReference(sourceSpriteSheet);

        if (allSprites == null || allSprites.Length == 0)
        {
            Debug.LogError("PlayerAttackSetupHelper: Could not discover sliced sprites. Make sure the sheet is imported as Sprite (2D and UI), Sprite Mode: Multiple, and sliced.");
            return;
        }

        List<Sprite> frames = new List<Sprite>();
        for (int i = 0; i < allSprites.Length; i++)
        {
            Sprite sprite = allSprites[i];
            if (sprite != null && sprite.name.StartsWith(framePrefix, StringComparison.OrdinalIgnoreCase))
            {
                frames.Add(sprite);
            }
        }

        if (frames.Count == 0)
        {
            Debug.LogError($"PlayerAttackSetupHelper: No sprites found with prefix '{framePrefix}'.");
            return;
        }

        frames.Sort((a, b) => ExtractTrailingNumber(a.name).CompareTo(ExtractTrailingNumber(b.name)));

        attackData.animationFrames = frames.ToArray();
        if (attackData.animationFrames.Length > 0)
        {
            attackData.sprite = attackData.animationFrames[0];
        }

        Debug.Log($"PlayerAttackSetupHelper: Filled {targetAttackType} with {attackData.animationFrames.Length} frame(s) using prefix '{framePrefix}'.");
    }

    private static AttackData FindOrCreateAttackData(PlayerAttacks attacks, AttackType type)
    {
        if (attacks == null)
            return null;

        if (attacks.attacks != null)
        {
            for (int i = 0; i < attacks.attacks.Length; i++)
            {
                if (attacks.attacks[i] != null && attacks.attacks[i].type == type)
                {
                    return attacks.attacks[i];
                }
            }
        }

        AttackData created = new AttackData
        {
            type = type,
            animationFps = 16f,
            damage = 5f,
            knockback = 3f,
            duration = 0.25f,
            hitboxSize = new Vector2(0.9f, 0.8f),
            hitboxOffset = new Vector2(0.6f, 0f),
            startupTime = 0.06f,
            endLag = 0.1f
        };

        int oldLen = attacks.attacks != null ? attacks.attacks.Length : 0;
        AttackData[] next = new AttackData[oldLen + 1];

        for (int i = 0; i < oldLen; i++)
            next[i] = attacks.attacks[i];

        next[oldLen] = created;
        attacks.attacks = next;

        return created;
    }

    private static int ExtractTrailingNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
            return int.MaxValue;

        int end = value.Length - 1;
        int start = end;

        while (start >= 0 && char.IsDigit(value[start]))
        {
            start--;
        }

        start++;

        if (start <= end && int.TryParse(value.Substring(start, end - start + 1), out int parsed))
        {
            return parsed;
        }

        return int.MaxValue;
    }

    private static Sprite[] TryCollectSubSpritesFromSceneReference(Sprite reference)
    {
        if (reference == null || reference.texture == null)
            return Array.Empty<Sprite>();

#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(reference);
        if (!string.IsNullOrEmpty(path))
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> fromAssetPath = new List<Sprite>();

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    fromAssetPath.Add(sprite);
                }
            }

            if (fromAssetPath.Count > 0)
                return fromAssetPath.ToArray();
        }
#endif

        // Runtime-safe fallback: includes only sprites already loaded from this sheet.
        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        List<Sprite> matching = new List<Sprite>();

        for (int i = 0; i < loadedSprites.Length; i++)
        {
            Sprite sprite = loadedSprites[i];
            if (sprite != null && sprite.texture == reference.texture)
            {
                matching.Add(sprite);
            }
        }

        return matching.ToArray();
    }
}

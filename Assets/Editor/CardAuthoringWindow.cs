using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ANTK.Game.StateEngine.StatusSystem;
using UnityEditor;
using UnityEngine;

public sealed class CardAuthoringWindow : EditorWindow
{
    private static readonly CardType[] CardTypes = { CardType.Attack, CardType.Defense };
    private readonly List<EffectDraft> _effects = new List<EffectDraft>();
    private readonly List<StatusDefinition> _statusDefinitions = new List<StatusDefinition>();
    private Vector2 _scrollPosition;
    private string _title = string.Empty;
    private string _baseCost = "0";
    private string _outputFolder = "Assets/Cards";
    private CardType _cardType = CardType.Attack;
    private string _errorMessage;

    [MenuItem("Tools/ANTK/Cards/Card Authoring")]
    public static void Open()
    {
        GetWindow<CardAuthoringWindow>("Card Authoring");
    }

    private void OnEnable()
    {
        RefreshStatusDefinitions();
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        _title = EditorGUILayout.TextField("Card name", _title);
        int cardTypeIndex = Array.IndexOf(CardTypes, _cardType);
        if (cardTypeIndex < 0)
        {
            cardTypeIndex = 0;
        }
        cardTypeIndex = EditorGUILayout.Popup("Card type", cardTypeIndex, new[] { "Attack", "Defense" });
        _cardType = CardTypes[cardTypeIndex];
        _baseCost = EditorGUILayout.TextField("Base cost", _baseCost);
        _outputFolder = EditorGUILayout.TextField("Existing asset folder", _outputFolder);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

        for (int index = 0; index < _effects.Count; index++)
        {
            if (DrawEffect(index))
            {
                _effects.RemoveAt(index);
                break;
            }
        }

        if (GUILayout.Button("Add effect"))
        {
            _effects.Add(new EffectDraft());
            _errorMessage = null;
        }

        if (GUILayout.Button("Refresh status list"))
        {
            RefreshStatusDefinitions();
        }

        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
        }

        if (GUILayout.Button("Generate card assets"))
        {
            GenerateAssets();
        }

        EditorGUILayout.EndScrollView();
    }

    private bool DrawEffect(int index)
    {
        EffectDraft effect = _effects[index];
        bool removeEffect = false;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Effect {index + 1}", EditorStyles.boldLabel);
        if (GUILayout.Button("Remove", GUILayout.Width(70f)))
        {
            removeEffect = true;
        }
        EditorGUILayout.EndHorizontal();

        effect.Type = (CardEffectType)EditorGUILayout.EnumPopup("Effect", effect.Type);
        effect.Target = (CardEffectTarget)EditorGUILayout.Popup(
            "Target",
            (int)effect.Target,
            new[] { "Self / player", "Opponent" });

        if (effect.Type == CardEffectType.Damage)
        {
            effect.DamageAmount = EditorGUILayout.TextField("Damage amount", effect.DamageAmount);
        }
        else
        {
            DrawStatusDropdown(effect);
            effect.Stacks = EditorGUILayout.TextField("Stacks", effect.Stacks);
            effect.DurationTurns = EditorGUILayout.TextField("Duration turns (-1 = permanent)", effect.DurationTurns);
        }

        EditorGUILayout.EndVertical();
        return removeEffect;
    }

    private void DrawStatusDropdown(EffectDraft effect)
    {
        string[] labels = new string[_statusDefinitions.Count];
        int selectedIndex = -1;
        for (int index = 0; index < _statusDefinitions.Count; index++)
        {
            StatusDefinition statusDefinition = _statusDefinitions[index];
            labels[index] = statusDefinition ? statusDefinition.Name : "<missing>";
            if (statusDefinition == effect.StatusDefinition)
            {
                selectedIndex = index;
            }
        }

        if (_statusDefinitions.Count == 0)
        {
            EditorGUILayout.Popup("Status", 0, new[] { "No status definitions found" });
            effect.StatusDefinition = null;
            return;
        }

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        selectedIndex = EditorGUILayout.Popup("Status", selectedIndex, labels);
        effect.StatusDefinition = _statusDefinitions[selectedIndex];
    }

    private void RefreshStatusDefinitions()
    {
        _statusDefinitions.Clear();
        string[] assetGuids = AssetDatabase.FindAssets("t:StatusDefinition");
        foreach (string assetGuid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            StatusDefinition statusDefinition = AssetDatabase.LoadAssetAtPath<StatusDefinition>(assetPath);
            if (statusDefinition)
            {
                _statusDefinitions.Add(statusDefinition);
            }
        }

        _statusDefinitions.Sort((left, right) =>
            string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
    }

    private void GenerateAssets()
    {
        if (!TryValidate(out int baseCost, out string folderPath, out List<CardEffect> effects))
        {
            return;
        }

        string fileStem = Regex.Replace(_title.Trim(), @"[^a-zA-Z0-9_-]+", "_").Trim('_');
        if (string.IsNullOrEmpty(fileStem))
        {
            fileStem = "Card";
        }

        string cardPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{fileStem}.asset");
        string uniqueStem = Path.GetFileNameWithoutExtension(cardPath);
        string behaviourPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{folderPath}/{uniqueStem}_Behaviour.asset");
        DataDrivenCardBehaviour behaviour = CreateInstance<DataDrivenCardBehaviour>();
        CardDefinition definition = CreateInstance<CardDefinition>();
        bool behaviourCreated = false;
        bool definitionCreated = false;

        try
        {
            ConfigureBehaviour(behaviour, effects);
            AssetDatabase.CreateAsset(behaviour, behaviourPath);
            behaviourCreated = true;

            ConfigureCardDefinition(definition, behaviour, baseCost);
            AssetDatabase.CreateAsset(definition, cardPath);
            definitionCreated = true;

            EditorUtility.SetDirty(behaviour);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
            _errorMessage = null;
        }
        catch (Exception exception)
        {
            if (definitionCreated)
            {
                AssetDatabase.DeleteAsset(cardPath);
            }
            if (behaviourCreated)
            {
                AssetDatabase.DeleteAsset(behaviourPath);
            }

            if (!definitionCreated)
            {
                DestroyImmediate(definition);
            }
            if (!behaviourCreated)
            {
                DestroyImmediate(behaviour);
            }
            Debug.LogException(exception);
            _errorMessage = $"Asset generation failed: {exception.Message}";
        }
    }

    private bool TryValidate(
        out int baseCost,
        out string folderPath,
        out List<CardEffect> effects)
    {
        baseCost = 0;
        folderPath = _outputFolder.Trim().Replace('\\', '/').TrimEnd('/');
        effects = new List<CardEffect>();

        if (string.IsNullOrWhiteSpace(_title))
        {
            return SetValidationError("Enter a card name.");
        }
        if (!CardTypeIsSupported(_cardType))
        {
            return SetValidationError("Select a supported card type.");
        }
        if (!int.TryParse(_baseCost, NumberStyles.None, CultureInfo.InvariantCulture, out baseCost) ||
            baseCost < 0)
        {
            return SetValidationError("Base cost must be a non-negative whole number.");
        }
        if (!folderPath.StartsWith("Assets", StringComparison.Ordinal) ||
            (folderPath.Length > "Assets".Length && folderPath["Assets".Length] != '/'))
        {
            return SetValidationError("Output folder must be an existing folder inside Assets.");
        }
        foreach (string segment in folderPath.Split('/'))
        {
            if (segment == "." || segment == "..")
            {
                return SetValidationError("Output folder cannot contain relative path segments.");
            }
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            return SetValidationError($"Asset folder '{folderPath}' does not exist.");
        }
        if (_effects.Count == 0)
        {
            return SetValidationError("Add at least one effect.");
        }

        foreach (EffectDraft draft in _effects)
        {
            if (draft.Type == CardEffectType.Damage)
            {
                if (!float.TryParse(
                        draft.DamageAmount,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float damageAmount) ||
                    damageAmount <= 0f ||
                    float.IsNaN(damageAmount) ||
                    float.IsInfinity(damageAmount))
                {
                    return SetValidationError("Damage amount must be a finite number greater than zero.");
                }

                effects.Add(new CardEffect(draft.Type, draft.Target, damageAmount));
                continue;
            }

            if (draft.Type != CardEffectType.ApplyStatus)
            {
                return SetValidationError("Select a supported effect.");
            }
            if (!draft.StatusDefinition || string.IsNullOrWhiteSpace(draft.StatusDefinition.Id))
            {
                return SetValidationError("Select a status with a valid ID.");
            }
            if (!int.TryParse(draft.Stacks, NumberStyles.None, CultureInfo.InvariantCulture, out int stacks) ||
                stacks < 1)
            {
                return SetValidationError("Status stacks must be a positive whole number.");
            }
            if (!int.TryParse(
                    draft.DurationTurns,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out int durationTurns) ||
                durationTurns < -1)
            {
                return SetValidationError("Status duration must be -1 or a non-negative whole number.");
            }

            effects.Add(new CardEffect(
                draft.Type,
                draft.Target,
                statusDefinition: draft.StatusDefinition,
                stacks: stacks,
                durationTurns: durationTurns));
        }

        _errorMessage = null;
        return true;
    }

    private static bool CardTypeIsSupported(CardType cardType)
    {
        foreach (CardType supportedType in CardTypes)
        {
            if (supportedType == cardType)
            {
                return true;
            }
        }

        return false;
    }

    private bool SetValidationError(string message)
    {
        _errorMessage = message;
        return false;
    }

    private static void ConfigureBehaviour(
        DataDrivenCardBehaviour behaviour,
        IReadOnlyList<CardEffect> effects)
    {
        SerializedObject serializedBehaviour = new SerializedObject(behaviour);
        SerializedProperty serializedEffects = serializedBehaviour.FindProperty("_effects");
        serializedEffects.arraySize = effects.Count;

        for (int index = 0; index < effects.Count; index++)
        {
            CardEffect effect = effects[index];
            SerializedProperty serializedEffect = serializedEffects.GetArrayElementAtIndex(index);
            serializedEffect.FindPropertyRelative("_type").enumValueIndex = (int)effect.Type;
            serializedEffect.FindPropertyRelative("_target").enumValueIndex = (int)effect.Target;
            serializedEffect.FindPropertyRelative("_damageAmount").floatValue = effect.DamageAmount;
            serializedEffect.FindPropertyRelative("_statusDefinition").objectReferenceValue =
                effect.StatusDefinition;
            serializedEffect.FindPropertyRelative("_stacks").intValue = effect.Stacks;
            serializedEffect.FindPropertyRelative("_durationTurns").intValue = effect.DurationTurns;
        }

        serializedBehaviour.ApplyModifiedPropertiesWithoutUndo();
    }

    private void ConfigureCardDefinition(
        CardDefinition definition,
        DataDrivenCardBehaviour behaviour,
        int baseCost)
    {
        SerializedObject serializedDefinition = new SerializedObject(definition);
        serializedDefinition.FindProperty("_id").stringValue = Guid.NewGuid().ToString("N");
        serializedDefinition.FindProperty("_title").stringValue = _title.Trim();
        serializedDefinition.FindProperty("_type").intValue = (int)_cardType;
        serializedDefinition.FindProperty("_behaviour").objectReferenceValue = behaviour;
        serializedDefinition.FindProperty("_baseCost").intValue = baseCost;
        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
    }

    private sealed class EffectDraft
    {
        public CardEffectType Type;
        public CardEffectTarget Target;
        public string DamageAmount = "1";
        public StatusDefinition StatusDefinition;
        public string Stacks = "1";
        public string DurationTurns = "2";
    }
}

// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.EditorUtils;
using EDIVE.Forms.Questions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR && CSV_HELPER
using System.Globalization;
using System.IO;
using Cysharp.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
#endif

namespace EDIVE.Forms
{
    public class FormDefinition : AUniqueDefinition
    {
        [SerializeReference]
        [HideReferenceObjectPicker]
        [EnhancedValueDropdown("GetQuestionsDropdown", DrawDropdownForListElements = false)]
        [CustomValueDrawer("CustomQuestionDrawer")]
        private List<AFormQuestion> _Questions = new();
        
        [SerializeField]
        private VisualPreset _Visual;
        
        public List<AFormQuestion> Questions => _Questions;
        public VisualPreset Visual => _Visual;
        
#if UNITY_EDITOR
        [UsedImplicitly]
        private IEnumerable GetQuestionsDropdown()
        {
            return TypeCacheUtils.GetAssignableClassesOfType<AFormQuestion>().Select(t => new ValueDropdownItem(t.EditorLabel, t));
        }
        
        [UsedImplicitly]
        private AFormQuestion CustomQuestionDrawer(AFormQuestion value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            if (value != null) 
                GUILayout.Label(value.EditorLabel, EditorStyles.boldLabel);
            callNextDrawer(label);
            return value;
        }
#endif
        
#if UNITY_EDITOR && CSV_HELPER
        [Button]
        private async UniTask ImportFromCSV()
        {
            var filePath = EditorUtility.OpenFilePanel("Select CSV file", "", "csv");
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            };

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, config);

                if (!await csv.ReadAsync() || !csv.ReadHeader())
                {
                    EditorUtility.DisplayDialog("Import Error", "Could not read header row.", "OK");
                    return;
                }

                _Questions ??= new List<AFormQuestion>();
                _Questions.Clear();

                string[] optionLetters = { "A", "B", "C", "D", "E" };

                while (await csv.ReadAsync())
                {
                    var id = csv.GetField(0)?.Trim();
                    var description = csv.GetField(1)?.Trim();

                    if (string.IsNullOrEmpty(description))
                        continue;

                    var correctAnswer = csv.GetField(7)?.Trim().ToUpper();

                    var options = new List<SimpleQuestionOption>();
                    for (int i = 0; i < 5; i++)
                    {
                        var optionText = csv.GetField(2 + i)?.Trim();
                        if (string.IsNullOrEmpty(optionText))
                            continue;
                        var isCorrect = optionLetters[i] == correctAnswer;
                        options.Add(new SimpleQuestionOption(optionLetters[i], optionText, isCorrect));
                    }

                    var questionId = $"Q{id}";
                    _Questions.Add(new SimpleOptionsQuestion(questionId, description, options));
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Import Error", ex.Message, "OK");
                return;
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}

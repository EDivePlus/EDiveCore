// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Controllers;
using EDIVE.Forms.Questions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Forms
{
    public class FormController : MonoBehaviour
    {
        [SerializeField]
        private FormDefinition _Definition;
        
        [SerializeReference]
        private IActivation _StartActivation;

        [SerializeReference]
        [PropertySpace]
        private IActivation _NextActivation;
        
        [SerializeReference]
        private IActivation _PreviousActivation;
        
        [PropertySpace]
        [SerializeField]
        private TMP_Text _QuestionNumberText;
        
        [SerializeField]
        private TMP_Text _TotalQuestionsText;
        
        [PropertySpace]
        [ValidateMultiState(typeof(FormControllerState))]
        [SerializeField]
        private AMultiState _FormState;

        [SerializeField]
        private AMultiState _ActiveQuestionControllerState;
        
        [SerializeField]
        [EnhancedBoxGroup("Answer Validation")]
        [Tooltip("Switches to a the 'AnswerValidation' state after answering a question, if the question requires validation.")]
        private bool _ShowAnswerValidation;
        
        [SerializeField]
        [EnhancedBoxGroup("Answer Validation")]
        [ShowIf("_ShowAnswerValidation")]
        private AToggleState _IsAnswerValidToggle;
        
        private AFormQuestion CurrentQuestion => _Definition.Questions[_currentQuestionIndex];
        
        private FormAnswerBundle _currentAnswers;
        
        private int _currentQuestionIndex = -1;
        private AFormQuestionController _currentQuestionController;
        private FormControllerState _currentState;

        private void OnEnable()
        {
            _StartActivation?.RegisterActivationListener(StartForm);
            _NextActivation?.RegisterActivationListener(ShowNextQuestion);
            _PreviousActivation?.RegisterActivationListener(ShowPreviousQuestion);
            
            if (_Definition != null)
                Initialize(_Definition);
        }

        private void OnDisable()
        {
            _StartActivation?.UnregisterActivationListener(StartForm);
            _NextActivation?.UnregisterActivationListener(ShowNextQuestion);
            _PreviousActivation?.UnregisterActivationListener(ShowPreviousQuestion);
        }
        
        public void Initialize(FormDefinition definition)
        {
            _Definition = definition;
            _currentState = FormControllerState.Initial;
        }
        
        public void StartForm()
        {
            _currentAnswers = new FormAnswerBundle
            {
                ParticipantID = Guid.NewGuid().ToString()
            };
            
            _currentState = FormControllerState.Question;
            ShowQuestion(0);
        }
        
        public void EndForm()
        {
            SaveToFile();
            
            _currentState = FormControllerState.Completed;
            UpdateUIDisplay();
        }

        public void ReportAnswer<TAnswer>(string questionID, TAnswer answer) where TAnswer : AFormAnswer
        {
            _currentAnswers.Set(questionID, answer);
            UpdateUIDisplay();
        }

        public void RequestNextQuestion()
        {
            if (_ShowAnswerValidation && _currentState == FormControllerState.Question)
            {
                ShowAnswerValidation();
                return;
            }
            ShowNextQuestion();
        }

        private void ShowNextQuestion()
        {
            if (_currentQuestionIndex + 1 >= _Definition.Questions.Count)
            {
                EndForm();
                return;
            }
            
            ShowQuestion(_currentQuestionIndex + 1);
        }

        private void ShowPreviousQuestion()
        {
            if (_currentQuestionIndex - 1 < 0)
            {
                Debug.LogWarning("Already at the first question, cannot go back.");
                return;
            }
            
            ShowQuestion(_currentQuestionIndex - 1);
        }

        private void ShowQuestion(int questionIndex)
        {
            if (questionIndex < 0 || questionIndex >= _Definition.Questions.Count)
            {
                Debug.LogError("Question index out of range.");
                return;
            }
            
            if (_currentQuestionController != null)
                Destroy(_currentQuestionController.gameObject);
            
            _currentQuestionIndex = questionIndex;
            UpdateUIDisplay();
        }

        private void ShowAnswerValidation()
        {
            _currentState = FormControllerState.AnswerValidation;
            // todo
            var isQuestionValid = false;
            
            if (_IsAnswerValidToggle)
                _IsAnswerValidToggle.SetState(isQuestionValid);
        }

        private void UpdateUIDisplay()
        {
            if (_QuestionNumberText) 
                _QuestionNumberText.text = (_currentQuestionIndex + 1).ToString();
            
            if (_TotalQuestionsText) 
                _TotalQuestionsText.text = _Definition.Questions.Count.ToString();

            if (_FormState)
                _FormState.SetState(_currentState);
        }
        
        private bool IsQuestionAnswered(int questionIndex)
        {
            var questionID = _Definition.Questions[questionIndex].UniqueID;
            return _currentAnswers.TryGet(questionID, out var answer) && answer != null;
        }
        
        private void SaveToFile()
        {
            var fileName = $"Form_{_Definition.name}_{_currentAnswers.ParticipantID}";
            var filePath = GetUniqueFilePath(fileName, "json");
            var json = JsonConvert.SerializeObject(_currentAnswers, Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"Form answers saved to {filePath}");
        }

        private static string GetUniqueFilePath(string baseName, string extension)
        {
            var directory = Application.persistentDataPath;
            var filePath = System.IO.Path.Combine(directory, $"{baseName}.{extension}");
            var counter = 1;
            while (System.IO.File.Exists(filePath))
            {
                filePath = System.IO.Path.Combine(directory, $"{baseName}_{counter}.{extension}");
                counter++;
            }
            return filePath;
        }
    }
    
    public enum FormControllerState
    {
        Initial,
        Question,
        AnswerValidation,
        Completed
    }
}

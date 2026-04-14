// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections.Generic;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Controllers;
using EDIVE.Forms.Questions;
using EDIVE.NativeUtils;
using EDIVE.StateHandling.MultiStates;
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
        private IActivation _NextActivation;
        
        [SerializeReference]
        private IActivation _PreviousActivation;
        
        [SerializeField]
        private TMP_Text _QuestionNumberText;
        
        [SerializeField]
        private TMP_Text _TotalQuestionsText;
        
        [ValidateMultiState(typeof(FormControllerState))]
        [SerializeField]
        private AMultiState _FormState;
        
        [SerializeField]
        private List<AFormQuestionController> _AvailableControllers;
        
        private AFormQuestionController _currentQuestionController;
        private FormAnswerBundle _currentAnswers;
        private FormControllerState _currentState;
        
        public event Action<FormDefinition> FormChanged;
        public event Action<AFormQuestion> CurrentQuestionChanged;
        public event Action<FormAnswerBundle> AnswersChanged;

        public AFormQuestion CurrentQuestion { get; private set; }
        private int _currentQuestionIndex = -1;

        private void Awake()
        {
            if (_Definition != null)
                Initialize(_Definition);
        }

        private void OnDestroy()
        {
            Terminate();
        }

        private void OnEnable()
        {
            _StartActivation?.RegisterActivationListener(StartForm);
            _NextActivation?.RegisterActivationListener(ShowNextQuestion);
            _PreviousActivation?.RegisterActivationListener(ShowPreviousQuestion);
        }

        private void OnDisable()
        {
            _StartActivation?.UnregisterActivationListener(StartForm);
            _NextActivation?.UnregisterActivationListener(ShowNextQuestion);
            _PreviousActivation?.UnregisterActivationListener(ShowPreviousQuestion);
        }
        
        [Button]
        public void Initialize(FormDefinition definition)
        {
            _Definition = definition;
            _currentState = FormControllerState.Initial;
            FormChanged?.Invoke(_Definition);
        }

        public void Terminate()
        {
            if (_currentQuestionController != null)
            {
                _currentQuestionController.AnswerChanged -= OnQuestionAnswerChanged;
                _currentQuestionController.Terminate();
            }
        }

        [Button]
        public void StartForm()
        {
            _currentAnswers = new FormAnswerBundle(Guid.NewGuid().ToString());
            _currentState = FormControllerState.Question;
            TrySetQuestion(0);
        }
        
        [Button]
        public void EndForm()
        {
            SaveToFile();
            
            _currentState = FormControllerState.Completed;
            UpdateUIDisplay();
        }
        
        [Button]
        public void ShowNextQuestion()
        {
            if (_currentQuestionIndex + 1 >= _Definition.Questions.Count)
            {
                EndForm();
                return;
            }
            TrySetQuestion(_currentQuestionIndex + 1);
        }

        [Button]
        public void ShowPreviousQuestion()
        {
            if (_currentQuestionIndex - 1 < 0)
            {
                Debug.LogWarning("Already at the first question, cannot go back.");
                return;
            }
            
            TrySetQuestion(_currentQuestionIndex - 1);
        }
        
        private bool TrySetQuestion(int questionIndex)
        {
            if (questionIndex < 0 || questionIndex >= _Definition.Questions.Count)
            {
                Debug.LogError("Question index out of range");
                return false;
            }

            if (_currentQuestionController != null)
            {
                _currentQuestionController.AnswerChanged -= OnQuestionAnswerChanged;
                _currentQuestionController.Terminate();
            }
               
            
            _currentQuestionIndex = questionIndex;
            CurrentQuestion = _Definition.Questions[_currentQuestionIndex];

            if (!TryGetControllerForQuestion(CurrentQuestion, out var controller))
            {
                Debug.LogError($"No suitable controller for question '{CurrentQuestion.ID}'");
                return false;
            }

            _currentQuestionController = controller;
            _currentQuestionController.Initialize(CurrentQuestion);
            _currentQuestionController.AnswerChanged += OnQuestionAnswerChanged;
            
            UpdateUIDisplay();
            CurrentQuestionChanged?.Invoke(CurrentQuestion);
            return true;
        }
        
        private void OnQuestionAnswerChanged(AFormAnswer answer)
        {
            if (CurrentQuestion != null)
            {
                _currentAnswers.Set(CurrentQuestion.ID, answer);
                AnswersChanged?.Invoke(_currentAnswers);
            }
        }
        
        private bool IsQuestionAnswered(AFormQuestion question)
        {
            return question != null && _currentAnswers.TryGet(question.ID, out var answer) && answer != null;
        }

        public bool TryGetControllerForQuestion(AFormQuestion question, out AFormQuestionController controller)
        {
            return _AvailableControllers.TryGetFirst(c => c != null && c.IsSuitableFor(question), out controller);
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

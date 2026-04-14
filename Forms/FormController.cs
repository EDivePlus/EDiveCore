// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections.Generic;
using System.IO;
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
        
        [ValidateMultiState(typeof(FormStateType))]
        [SerializeField]
        private AMultiState _FormState;
        
        [SerializeField]
        private List<AFormQuestionController> _AvailableControllers;
        
        private AFormQuestionController _currentQuestionController;
        private FormAnswerBundle _currentAnswers;
        private FormStateType _currentFormState;
        
        public event Action<FormDefinition> FormChanged;
        public event Action<FormStateType> FormStateChanged;
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
            FormChanged?.Invoke(_Definition);
            SetFormState(FormStateType.Initial);
        }

        public void Terminate()
        {
            if (_currentQuestionController != null)
            {
                _currentQuestionController.AnswerChanged -= SetAnswerForCurrentQuestion;
                _currentQuestionController.Terminate();
            }
        }

        [Button]
        public void StartForm()
        {
            _currentAnswers = new FormAnswerBundle(Guid.NewGuid().ToString());
            SetFormState(FormStateType.Question);
            TrySetQuestion(0);
        }
        
        [Button]
        public void EndForm()
        {
            SaveAnswersToFile();
            SetFormState(FormStateType.Completed);
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

        private void SetFormState(FormStateType state)
        {
            _currentFormState = state;
            FormStateChanged?.Invoke(_currentFormState);
        }
        
        public bool TrySetQuestion(int questionIndex)
        {
            if (questionIndex < 0 || questionIndex >= _Definition.Questions.Count)
            {
                Debug.LogError("Question index out of range");
                return false;
            }

            if (_currentQuestionController != null)
            {
                _currentQuestionController.AnswerChanged -= SetAnswerForCurrentQuestion;
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
            _currentQuestionController.AnswerChanged += SetAnswerForCurrentQuestion;
            
            UpdateUIDisplay();
            CurrentQuestionChanged?.Invoke(CurrentQuestion);
            return true;
        }
        
        public void SetAnswerForCurrentQuestion(AFormAnswer answer)
        {
            if (CurrentQuestion == null)
            {
                Debug.LogError("No current question to set answer for.");
                return;
            }
            
            _currentAnswers.Set(CurrentQuestion.ID, answer);
            AnswersChanged?.Invoke(_currentAnswers);
        }
        
        public bool IsQuestionAnswered(AFormQuestion question)
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
                _FormState.SetState(_currentFormState);
        }
        
        public void SaveAnswersToFile()
        {
            var fileName = $"Form_{_Definition.UniqueID}_{_currentAnswers.ParticipantID}";
            var filePath = Path.Combine(PathUtility.GetRootAppDataPath("Forms"), $"{fileName}.json");
            PathUtility.EnsurePathExists(filePath);
            var json = JsonConvert.SerializeObject(_currentAnswers, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Debug.Log($"Form answers saved to {filePath}");
        }
    }
    
    public enum FormStateType
    {
        Initial,
        Question,
        Completed
    }
}

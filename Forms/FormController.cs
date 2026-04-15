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
        
        [SerializeReference]
        private IActivation _RestartActivation;
        
        [SerializeField]
        private TMP_Text _QuestionNumberText;
        
        [SerializeField]
        private TMP_Text _TotalQuestionsText;
        
        [ValidateMultiState(typeof(FormStateType))]
        [SerializeField]
        private AMultiState _FormState;
        
        [SerializeField]
        private List<AFormQuestionController> _AvailableControllers;
        
        public event Action<FormStateType> FormStateChanged;
        public event Action<int, AFormQuestion> CurrentQuestionChanged;
        public event Action<string, AFormAnswer> AnswerChanged;
        
        public AFormQuestionController CurrentQuestionController { get; private set; }
        public FormAnswerBundle CurrentAnswers { get; private set; } = new(Guid.NewGuid().ToString());
        public FormStateType CurrentFormState { get; private set; }
        public AFormQuestion CurrentQuestion { get; private set; }
        public int CurrentQuestionIndex { get; private set; } = -1;
        
        private void OnDestroy()
        {
            Terminate();
        }

        private void OnEnable()
        {
            _StartActivation?.RegisterActivationListener(StartForm);
            _NextActivation?.RegisterActivationListener(ShowNextQuestion);
            _PreviousActivation?.RegisterActivationListener(ShowPreviousQuestion);
            _RestartActivation?.RegisterActivationListener(Initialize);
        }

        private void OnDisable()
        {
            _StartActivation?.UnregisterActivationListener(StartForm);
            _NextActivation?.UnregisterActivationListener(ShowNextQuestion);
            _PreviousActivation?.UnregisterActivationListener(ShowPreviousQuestion);
            _RestartActivation?.UnregisterActivationListener(Initialize);
        }
        
        [Button]
        public void Initialize()
        {
            CurrentAnswers.Clear();
            SetFormState(FormStateType.Initial);
            TrySetQuestion(-1);
        }
        
        public void Terminate()
        {

            if (CurrentQuestionController != null)
            {
                CurrentQuestionController.AnswerChanged -= OnCurrentAnswerChanged;
                CurrentQuestionController.Terminate();
            }
        }

        [Button]
        public void StartForm()
        {
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
            if (CurrentQuestionIndex + 1 >= _Definition.Questions.Count)
            {
                EndForm();
                return;
            }
            TrySetQuestion(CurrentQuestionIndex + 1);
        }

        [Button]
        public void ShowPreviousQuestion()
        {
            if (CurrentQuestionIndex - 1 < 0)
            {
                Debug.LogWarning("Already at the first question, cannot go back.");
                return;
            }
            
            TrySetQuestion(CurrentQuestionIndex - 1);
        }

        public void SetFormState(FormStateType state)
        {
            CurrentFormState = state;
            if (_FormState)
                _FormState.SetState(CurrentFormState);
            FormStateChanged?.Invoke(CurrentFormState);
        }

        public bool TrySetQuestion(int questionIndex)
        {
            if (CurrentQuestionIndex == questionIndex)
                return true;

            if (CurrentQuestionController != null)
            {
                CurrentQuestionController.AnswerChanged -= OnCurrentAnswerChanged;
                CurrentQuestionController.Terminate();
            }

            CurrentQuestionIndex = questionIndex;
            if (questionIndex < 0 || questionIndex >= _Definition.Questions.Count)
            {
                UpdateUIDisplay();
                CurrentQuestionChanged?.Invoke(CurrentQuestionIndex, CurrentQuestion);
                return false;
            }

            CurrentQuestion = _Definition.Questions[CurrentQuestionIndex];

            if (!TryGetControllerForQuestion(CurrentQuestion, out var controller))
            {
                Debug.LogError($"No suitable controller for question '{CurrentQuestion.ID}'");
                return false;
            }

            CurrentQuestionController = controller;
            CurrentQuestionController.Initialize(CurrentQuestion);
            if (CurrentAnswers.TryGet(CurrentQuestion.ID, out var existingAnswer))
                CurrentQuestionController.SetAnswer(existingAnswer);

            CurrentQuestionController.AnswerChanged += OnCurrentAnswerChanged;
            
            UpdateUIDisplay();
            CurrentQuestionChanged?.Invoke(CurrentQuestionIndex, CurrentQuestion);
            return true;
        }
        
        public void SetAnswerForQuestion(string questionID, AFormAnswer answer)
        {
            CurrentAnswers.Set(questionID, answer);
            if (CurrentQuestion != null && CurrentQuestion.ID == questionID)
            {
                CurrentQuestionController.AnswerChanged -= OnCurrentAnswerChanged;
                CurrentQuestionController.SetAnswer(answer);
                CurrentQuestionController.AnswerChanged += OnCurrentAnswerChanged;
            }
            AnswerChanged?.Invoke(questionID, answer);
        }
        
        private void OnCurrentAnswerChanged(AFormAnswer answer)
        {
            if (CurrentQuestion == null)
            {
                Debug.LogError("No current question to set answer for.");
                return;
            }
            
            CurrentAnswers.Set(CurrentQuestion.ID, answer);
            AnswerChanged?.Invoke(CurrentQuestion.ID, answer);
        }
        
        public bool IsQuestionAnswered(AFormQuestion question)
        {
            return question != null && CurrentAnswers.TryGet(question.ID, out var answer) && answer != null;
        }

        public bool TryGetControllerForQuestion(AFormQuestion question, out AFormQuestionController controller)
        {
            return _AvailableControllers.TryGetFirst(c => c != null && c.IsSuitableFor(question), out controller);
        }
        
        private void UpdateUIDisplay()
        {
            if (_QuestionNumberText) 
                _QuestionNumberText.text = (CurrentQuestionIndex + 1).ToString();
            
            if (_TotalQuestionsText) 
                _TotalQuestionsText.text = _Definition.Questions.Count.ToString();
        }
        
        public void SaveAnswersToFile()
        {
            var fileName = $"Form_{_Definition.UniqueID}_{CurrentAnswers.ParticipantID}";
            var filePath = Path.Combine(PathUtility.GetRootAppDataPath("Forms"), $"{fileName}.json");
            PathUtility.EnsurePathExists(filePath);
            var json = JsonConvert.SerializeObject(CurrentAnswers, Formatting.Indented);
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

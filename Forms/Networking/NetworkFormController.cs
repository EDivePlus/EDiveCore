// Author: František Holubec
// Created: 14.04.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using Newtonsoft.Json;
using UnityEngine;
using PurrNet;

namespace EDIVE.Forms.Networking
{
    [RequireComponent(typeof(FormController))]
    public class NetworkFormController : NetworkBehaviour
    {
        private FormController _formController;
        
        private readonly SyncVar<int> _questionIndex = new();
        private readonly SyncVar<FormStateType> _formState = new();
        private readonly SyncDictionary<string, string> _answers = new();
        
        public event Action<Dictionary<string, AFormAnswer>> AnswersChanged; 
        private Dictionary<string, AFormAnswer> _parsedAnswers = new();
        
        private static readonly JsonSerializerSettings JSON_SETTINGS = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };


        private void Awake()
        {
            _formController = GetComponent<FormController>();
        }
        
        protected override void OnSpawned(bool asServer)
        {
            if (asServer)
            {
                _questionIndex.value = _formController.CurrentQuestionIndex;
                _formState.value = _formController.CurrentFormState;
                if (_formController.CurrentAnswers != null)
                {
                    _parsedAnswers = _formController.CurrentAnswers.Answers.ToDictionary(entry => entry.Key, entry => entry.Value);
                    foreach (var (questionID, answer) in _formController.CurrentAnswers.Answers)
                    {
                        _answers[questionID] = JsonConvert.SerializeObject(answer, typeof(AFormAnswer), JSON_SETTINGS);
                    }
                }
            }
        }

        protected override void OnSpawned()
        {
            RegisterLocalEvents();
            _questionIndex.onChanged += OnSyncCurrentQuestionChanged;
            _formState.onChanged += OnSyncFormStateChanged;
            _answers.onChanged += OnSyncAnswersChanged;
        }

        protected override void OnDespawned()
        {
            UnregisterLocalEvents();
            _questionIndex.onChanged -= OnSyncCurrentQuestionChanged;
            _formState.onChanged -= OnSyncFormStateChanged;
            _answers.onChanged -= OnSyncAnswersChanged;
        }
        
        private void RegisterLocalEvents()
        {
            _formController.CurrentQuestionChanged += OnLocalCurrentQuestionChanged;
            _formController.FormStateChanged += OnLocalFormStateChanged;
            _formController.AnswerChanged += OnLocalAnswerChanged;
        }

        private void UnregisterLocalEvents()
        {
            _formController.CurrentQuestionChanged -= OnLocalCurrentQuestionChanged;
            _formController.FormStateChanged -= OnLocalFormStateChanged;
            _formController.AnswerChanged -= OnLocalAnswerChanged;
        }
        
        private void OnLocalAnswerChanged(string answerID, AFormAnswer answer)
        {
            SetAnswer(answerID, JsonConvert.SerializeObject(answer, typeof(AFormAnswer), JSON_SETTINGS));
        }

        private void OnLocalFormStateChanged(FormStateType formState)
        {
            SetFormState(formState);
        }

        private void OnLocalCurrentQuestionChanged(int questionIndex, AFormQuestion question)
        {
            SetQuestionIndex(questionIndex);
        }
        
        [ServerRpc(requireOwnership: false)]
        private void SetQuestionIndex(int questionIndex)
        {
            _questionIndex.value = questionIndex;
        }
        
        [ServerRpc(requireOwnership: false)]
        private void SetFormState(FormStateType formState)
        {
            _formState.value = formState;
        }
        
        [ServerRpc(requireOwnership: false)]
        private void SetAnswer(string questionID, string answerJson)
        {
            _answers[questionID] = answerJson;
        }
        
        private void OnSyncAnswersChanged(SyncDictionaryChange<string, string> change)
        {
            UnregisterLocalEvents();
            switch (change.operation)
            {
                case SyncDictionaryOperation.Added:
                case SyncDictionaryOperation.Set:
                {
                    var answer = JsonConvert.DeserializeObject<AFormAnswer>(change.value, JSON_SETTINGS);
                    _parsedAnswers[change.key] = answer;
                    _formController.SetAnswerForQuestion(change.key, answer);
                    break;
                }
                case SyncDictionaryOperation.Cleared:
                    _parsedAnswers.Clear();
                    break;
                case SyncDictionaryOperation.Removed:
                    _parsedAnswers.Remove(change.key);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            AnswersChanged?.Invoke(_parsedAnswers);
            RegisterLocalEvents();
        }

        private void OnSyncFormStateChanged(FormStateType next)
        {            
            UnregisterLocalEvents();
            _formController.SetFormState(next);
            RegisterLocalEvents();
        }

        private void OnSyncCurrentQuestionChanged(int next)
        {
            UnregisterLocalEvents();
            _formController.TrySetQuestion(next);
            RegisterLocalEvents();
        }
    }
}

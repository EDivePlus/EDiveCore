// Author: František Holubec
// Created: 14.04.2026

using System;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Forms.Networking
{
    [RequireComponent(typeof(FormController))]
    public class NetworkFormController : NetworkBehaviour
    {
        private FormController _formController;
        
        private readonly SyncVar<int> _questionIndex = new();
        private readonly SyncVar<FormStateType> _formState = new();
        private readonly SyncDictionary<string, string> _answers = new();

        private static readonly JsonSerializerSettings JSON_SETTINGS = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };


        private void Awake()
        {
            _formController = GetComponent<FormController>();
        }
        
        public override void OnStartServer()
        {
            _questionIndex.Value = _formController.CurrentQuestionIndex;
            _formState.Value = _formController.CurrentFormState;
            if (_formController.CurrentAnswers != null)
            {
                foreach (var (questionID, answer) in _formController.CurrentAnswers.Answers)
                {
                    _answers[questionID] = JsonConvert.SerializeObject(answer, typeof(AFormAnswer), JSON_SETTINGS);
                }
            }
        }

        public override void OnStartClient()
        {
            RegisterLocalEvents();
            _questionIndex.OnChange += OnSyncCurrentQuestionChanged;
            _formState.OnChange += OnSyncFormStateChanged;
            _answers.OnChange += OnSyncAnswersChanged;
        }
        
        public override void OnStopClient()
        {
            UnregisterLocalEvents();
            _questionIndex.OnChange -= OnSyncCurrentQuestionChanged;
            _formState.OnChange -= OnSyncFormStateChanged;
            _answers.OnChange -= OnSyncAnswersChanged;
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
        
        [ServerRpc(RequireOwnership = false)]
        private void SetQuestionIndex(int questionIndex)
        {
            _questionIndex.Value = questionIndex;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetFormState(FormStateType formState)
        {
            _formState.Value = formState;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetAnswer(string questionID, string answer)
        {
            _answers[questionID] = answer;
        }
        
        private void OnSyncAnswersChanged(SyncDictionaryOperation op, string key, string value, bool asServer)
        {
            UnregisterLocalEvents();
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                case SyncDictionaryOperation.Set:
                {
                    var answer = JsonConvert.DeserializeObject<AFormAnswer>(value, JSON_SETTINGS);
                    _formController.SetAnswerForQuestion(key, answer);
                    break;
                }
                case SyncDictionaryOperation.Clear:
                case SyncDictionaryOperation.Remove:
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
            RegisterLocalEvents();
        }

        private void OnSyncFormStateChanged(FormStateType prev, FormStateType next, bool asServer)
        {            
            UnregisterLocalEvents();
            _formController.SetFormState(next);
            RegisterLocalEvents();
        }

        private void OnSyncCurrentQuestionChanged(int prev, int next, bool asServer)
        {
            UnregisterLocalEvents();
            _formController.TrySetQuestion(next);
            RegisterLocalEvents();
        }
    }
}

// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.RemoteContent.UI
{
    public class RemoteContentLibraryController : MonoBehaviour
    {
        [SerializeField]
        private RemoteContentInfoDisplay _Prefab;

        [SerializeField]
        private Transform _ContentParent;

        [SerializeField]
        private int _PageSize = 20;

        [SerializeField]
        [EnhancedBoxGroup("Pagination", Color = "@ColorTools.Green", SpaceBefore = 8)]
        private Button _PreviousPageButton;

        [SerializeField]
        [EnhancedBoxGroup("Pagination")]
        private Button _NextPageButton;

        [SerializeField]
        [EnhancedBoxGroup("Pagination")]
        private TMP_Text _CurrentPageText;

        [SerializeField]
        [EnhancedBoxGroup("Pagination")]
        private TMP_Text _TotalPagesText;

        [SerializeField]
        [EnhancedBoxGroup("Filtering", Color = "@ColorTools.Cyan", SpaceBefore = 8)]
        private TMP_Dropdown _MediaTypeDropdown;

        [SerializeField]
        [EnhancedBoxGroup("Filtering")]
        private TMP_InputField _SearchInput;

        [SerializeField]
        private AToggleState _IsLoadingState;

        private readonly List<RemoteContentInfoDisplay> _spawned = new();
        private List<ContentMediaTypeResponse> _mediaTypes;
        private string _selectedMediaTypeKey;
        private int _currentPage;
        private int _totalCount;
        private bool _hasMore;
        private CancellationTokenSource _cts;
        
        private RemoteContentService _serviceHub;

        private void OnEnable()
        {
            if (_PreviousPageButton != null)
                _PreviousPageButton.onClick.AddListener(OnPrevClicked);
            if (_NextPageButton != null)
                _NextPageButton.onClick.AddListener(OnNextClicked);
            if (_MediaTypeDropdown != null)
                _MediaTypeDropdown.onValueChanged.AddListener(OnMediaTypeChanged);
            if (_SearchInput != null)
                _SearchInput.onEndEdit.AddListener(OnSearchSubmitted);

            InitializeAsync().Forget();
        }

        private void OnDisable()
        {
            CancelInFlight();

            if (_PreviousPageButton != null)
                _PreviousPageButton.onClick.RemoveListener(OnPrevClicked);
            if (_NextPageButton != null)
                _NextPageButton.onClick.RemoveListener(OnNextClicked);
            if (_MediaTypeDropdown != null)
                _MediaTypeDropdown.onValueChanged.RemoveListener(OnMediaTypeChanged);
            if (_SearchInput != null)
                _SearchInput.onEndEdit.RemoveListener(OnSearchSubmitted);

            ClearSpawned();
        }

        private async UniTaskVoid InitializeAsync()
        {
            _ContentParent.transform.DestroyChildren();
            
            var ct = ResetCancellation();
            try
            {
                _serviceHub = AppCore.Services.Get<ServiceHubManager>().RemoteContent;
                if (_MediaTypeDropdown != null)
                    await PopulateMediaTypesAsync(ct);
                await LoadPageAsync(0, ct);
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask PopulateMediaTypesAsync(CancellationToken ct)
        {
            var response = await _serviceHub.ListContentMediaTypesAsync(ct);
            if (!response.IsSuccess || response.Result == null)
            {
                Debug.LogWarning($"[RemoteContentLibrary] Failed to load media types: {response.ErrorMessage}");
                return;
            }

            _mediaTypes = response.Result.MediaTypes ?? new List<ContentMediaTypeResponse>();

            var options = new List<TMP_Dropdown.OptionData>(_mediaTypes.Count + 1)
            {
                new("All")
            };
            foreach (var mt in _mediaTypes)
                options.Add(new TMP_Dropdown.OptionData(string.IsNullOrEmpty(mt.DisplayName) ? mt.Key : mt.DisplayName));

            _MediaTypeDropdown.ClearOptions();
            _MediaTypeDropdown.AddOptions(options);
            _MediaTypeDropdown.SetValueWithoutNotify(0);
            _selectedMediaTypeKey = null;
        }

        private async UniTask LoadPageAsync(int page, CancellationToken ct)
        {
            if (_serviceHub == null)
                return;

            _IsLoadingState?.SetState(true);
            try
            {
                var search = _SearchInput != null ? _SearchInput.text : null;
                var response = await _serviceHub.ListOwnContentItemsAsync(_selectedMediaTypeKey, search, page, _PageSize, ct);

                if (ct.IsCancellationRequested)
                    return;

                if (!response.IsSuccess || response.Result == null)
                {
                    Debug.LogWarning($"[RemoteContentLibrary] Failed to load page {page}: {response.ErrorMessage}");
                    return;
                }

                var pagination = response.Result.Pagination;
                _currentPage = pagination?.Page ?? page;
                _totalCount = pagination?.TotalCount ?? 0;
                _hasMore = pagination?.HasMore ?? false;

                Repopulate(response.Result.Items);
                RefreshPaginationUi();
            }
            finally
            {
                if (this != null && isActiveAndEnabled)
                    _IsLoadingState?.SetState(false);
            }
        }

        private void Repopulate(List<ContentItemInfo> items)
        {
            ClearSpawned();
            if (items == null || _Prefab == null || _ContentParent == null)
                return;

            foreach (var item in items)
            {
                var display = Instantiate(_Prefab, _ContentParent);
                display.ApplyContentInfo(item);
                _spawned.Add(display);
            }
        }

        private void ClearSpawned()
        {
            foreach (var display in _spawned)
            {
                if (display == null)
                    continue;
                Destroy(display.gameObject);
            }
            _spawned.Clear();
        }

        private void RefreshPaginationUi()
        {
            var pageSize = Mathf.Max(1, _PageSize);
            var totalPages = Mathf.Max(1, Mathf.CeilToInt(_totalCount / (float)pageSize));

            if (_CurrentPageText != null)
                _CurrentPageText.text = (_currentPage + 1).ToString();
            if (_TotalPagesText != null)
                _TotalPagesText.text = totalPages.ToString();
            if (_PreviousPageButton != null)
                _PreviousPageButton.interactable = _currentPage > 0;
            if (_NextPageButton != null)
                _NextPageButton.interactable = _hasMore;
        }

        private void OnPrevClicked()
        {
            if (_currentPage <= 0)
                return;
            ReloadAsync(_currentPage - 1).Forget();
        }

        private void OnNextClicked()
        {
            if (!_hasMore)
                return;
            ReloadAsync(_currentPage + 1).Forget();
        }

        private void OnMediaTypeChanged(int idx)
        {
            _selectedMediaTypeKey = idx <= 0 || _mediaTypes == null || idx - 1 >= _mediaTypes.Count
                ? null
                : _mediaTypes[idx - 1].Key;
            ReloadAsync(0).Forget();
        }

        private void OnSearchSubmitted(string _)
        {
            ReloadAsync(0).Forget();
        }

        private async UniTaskVoid ReloadAsync(int page)
        {
            var ct = ResetCancellation();
            try
            {
                await LoadPageAsync(page, ct);
            }
            catch (OperationCanceledException) { }
        }

        private CancellationToken ResetCancellation()
        {
            CancelInFlight();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            return _cts.Token;
        }

        private void CancelInFlight()
        {
            if (_cts == null)
                return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}

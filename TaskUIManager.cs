using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;

public class TaskUIManager
{
    private GameObject _mainTaskPanel;
    private TMP_InputField _taskContentInput;
    private TMP_InputField _taskLocationInput;
    private TMP_InputField _taskDescriptionInput;
    private Button _addTaskButton;
    private Button _closeDetailsButton;

    private Toggle[] _sharedRiskToggles;
    private string[] _riskNames = new string[]
    {
        "Làm việc trên cao",
        "Làm việc trong không gian kín",
        "Tiếp xúc hóa chất",
        "Tiếp xúc bề mặt nóng",
        "Thiết bị có áp suất cao",
        "Có phát sinh tia lửa",
        "Làm việc với thiết bị điện",
        "Thiết bị quay, truyền động",
        "Có vật bay",
        "Làm việc với thiết bị nặng",
        "Làm việc với cẩu",
        "Rủi ro tràn dầu",
        "Làm việc với vật sắc nhọn"
    };

    private Transform _tasksListParent;
    private GameObject _taskItemPrefab;
    private TextMeshProUGUI _notificationText;

    private GameObject _loadingIndicatorPanel;
    private TextMeshProUGUI _loadingPercentageText;
    private Coroutine _loadingCoroutine;

    private Button _showTaskListButton;
    private GameObject _taskListPanel;
    private Button _showInputPanelButton;

    private Toggle _detailsStatusToggle;
    private TextMeshProUGUI _detailsStatusToggleLabel;
    private Button _markAsDoneButton;
    private Button _materialsButton;

    private TMP_Dropdown _statusFilterDropdown;

    private Transform _inProgressTasksListParent;

    private GameObject _confirmPopupPanel;
    private TextMeshProUGUI _confirmPopupText;
    private Button _confirmYesButton;
    private Button _confirmNoButton;

    public event Action<string, string, string, string[]> OnAddTaskClicked;
    public event Action OnCloseDetailsClicked;
    public event Action OnShowTaskListClicked;
    public event Action OnShowInputPanelClicked;
    public event Action<bool> OnDetailsStatusToggleChanged;
    public event Action OnMarkAsDoneClicked;
    public event Action OnMaterialsClicked;
    public event Action<int> OnStatusFilterChanged;
    public event Action<Dictionary<string, object>> OnTaskItemClicked;

    public event Action OnConfirmYesClicked;
    public event Action OnConfirmNoClicked;

    private MonoBehaviour _monoBehaviourContext;

    public TaskUIManager(
        GameObject mainTaskPanel, TMP_InputField taskContentInput, TMP_InputField taskLocationInput, TMP_InputField taskDescriptionInput,
        Button addTaskButton, Button closeDetailsButton, Toggle[] sharedRiskToggles, TextMeshProUGUI notificationText,
        GameObject loadingIndicatorPanel, TextMeshProUGUI loadingPercentageText,
        Button showTaskListButton, GameObject taskListPanel, Button showInputPanelButton,
        TMP_Dropdown statusFilterDropdown, Transform tasksListParent, GameObject taskItemPrefab,
        Transform inProgressTasksListParent,
        GameObject confirmPopupPanel, TextMeshProUGUI confirmPopupText, Button confirmYesButton, Button confirmNoButton
    )
    {
        _mainTaskPanel = mainTaskPanel;
        _taskContentInput = taskContentInput;
        _taskLocationInput = taskLocationInput;
        _taskDescriptionInput = taskDescriptionInput;
        _addTaskButton = addTaskButton;
        _closeDetailsButton = closeDetailsButton;
        _sharedRiskToggles = sharedRiskToggles;
        _notificationText = notificationText;
        _loadingIndicatorPanel = loadingIndicatorPanel;
        _loadingPercentageText = loadingPercentageText;
        _showTaskListButton = showTaskListButton;
        _taskListPanel = taskListPanel;
        _showInputPanelButton = showInputPanelButton;
        _statusFilterDropdown = statusFilterDropdown;
        _tasksListParent = tasksListParent;
        _taskItemPrefab = taskItemPrefab;
        _inProgressTasksListParent = inProgressTasksListParent;
        _confirmPopupPanel = confirmPopupPanel;
        _confirmPopupText = confirmPopupText;
        _confirmYesButton = confirmYesButton;
        _confirmNoButton = confirmNoButton;

        _monoBehaviourContext = mainTaskPanel.GetComponentInParent<TaskManager>();
        if (_monoBehaviourContext == null)
        {
            Debug.LogError("TaskUIManager needs a MonoBehaviour context to run coroutines. Please ensure TaskManager or another MonoBehaviour is active.");
        }

        SetupUIListeners();
    }

    private void SetupUIListeners()
    {
        if (_addTaskButton != null) _addTaskButton.onClick.AddListener(() =>
        {
            string content = _taskContentInput.text.Trim();
            string location = _taskLocationInput.text.Trim();
            string description = _taskDescriptionInput.text.Trim();
            List<string> selectedRisks = new List<string>();
            for (int i = 0; i < _sharedRiskToggles.Length; i++)
            {
                if (_sharedRiskToggles[i] != null && _sharedRiskToggles[i].isOn)
                {
                    selectedRisks.Add(_riskNames[i]);
                }
            }
            OnAddTaskClicked?.Invoke(content, location, description, selectedRisks.ToArray());
        });

        if (_closeDetailsButton != null) _closeDetailsButton.onClick.AddListener(() => OnCloseDetailsClicked?.Invoke());

        if (_showTaskListButton != null) _showTaskListButton.onClick.AddListener(() => OnShowTaskListClicked?.Invoke());

        if (_showInputPanelButton != null) _showInputPanelButton.onClick.AddListener(() => OnShowInputPanelClicked?.Invoke());

        if (_statusFilterDropdown != null)
        {
            _statusFilterDropdown.ClearOptions();
            _statusFilterDropdown.options.Add(new TMP_Dropdown.OptionData(TaskConstants.STATUS_ALL));
            _statusFilterDropdown.options.Add(new TMP_Dropdown.OptionData(TaskConstants.STATUS_PENDING));
            _statusFilterDropdown.options.Add(new TMP_Dropdown.OptionData(TaskConstants.STATUS_IN_PROGRESS));
            _statusFilterDropdown.options.Add(new TMP_Dropdown.OptionData(TaskConstants.STATUS_DONE));
            _statusFilterDropdown.value = 0;
            _statusFilterDropdown.onValueChanged.AddListener(index => OnStatusFilterChanged?.Invoke(index));
        }

        if (_confirmYesButton != null) _confirmYesButton.onClick.AddListener(() => OnConfirmYesClicked?.Invoke());
        if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(() => OnConfirmNoClicked?.Invoke());
    }

    public void InitializeUIState()
    {
        if (_taskListPanel != null) _taskListPanel.SetActive(false);
        if (_loadingIndicatorPanel != null) _loadingIndicatorPanel.SetActive(false);
        if (_mainTaskPanel != null) _mainTaskPanel.SetActive(false);
        
        if (_confirmPopupPanel != null) _confirmPopupPanel.SetActive(false);

        ShowInputView();
    }

    public void InitializeSharedRiskTogglesLabels()
    {
        if (_sharedRiskToggles.Length != _riskNames.Length)
        {
            Debug.LogError("Shared Risk Toggles array size does not match riskNames array in TaskUIManager.");
            return;
        }

        for (int i = 0; i < _sharedRiskToggles.Length; i++)
        {
            if (_sharedRiskToggles[i] != null)
            {
                TextMeshProUGUI toggleLabel = _sharedRiskToggles[i].GetComponentInChildren<TextMeshProUGUI>();
                if (toggleLabel != null)
                {
                    toggleLabel.text = _riskNames[i];
                }
            }
        }
        if (_detailsStatusToggleLabel != null) _detailsStatusToggleLabel.text = TaskConstants.STATUS_PENDING + " / " + TaskConstants.STATUS_IN_PROGRESS;
    }

    public void ShowInputView()
    {
        if (_taskContentInput != null) { _taskContentInput.interactable = true; _taskContentInput.text = ""; }
        if (_taskLocationInput != null) { _taskLocationInput.interactable = true; _taskLocationInput.text = ""; }
        if (_taskDescriptionInput != null) { _taskDescriptionInput.interactable = true; _taskDescriptionInput.text = ""; }

        if (_addTaskButton != null) _addTaskButton.gameObject.SetActive(true);
        if (_closeDetailsButton != null) _closeDetailsButton.gameObject.SetActive(false);
        if (_detailsStatusToggle != null) _detailsStatusToggle.gameObject.SetActive(false);
        if (_markAsDoneButton != null) _markAsDoneButton.gameObject.SetActive(false);
        if (_materialsButton != null) _materialsButton.gameObject.SetActive(false);

        foreach (Toggle toggle in _sharedRiskToggles)
        {
            if (toggle != null)
            {
                toggle.gameObject.SetActive(true);
                toggle.interactable = true;
                toggle.isOn = false;
            }
        }
    }

    public void ShowDetailsView()
    {
        if (_taskContentInput != null) _taskContentInput.interactable = false;
        if (_taskLocationInput != null) _taskLocationInput.interactable = false;
        if (_taskDescriptionInput != null) _taskDescriptionInput.interactable = false;

        if (_addTaskButton != null) _addTaskButton.gameObject.SetActive(false);
        if (_closeDetailsButton != null) _closeDetailsButton.gameObject.SetActive(true);
        if (_detailsStatusToggle != null) _detailsStatusToggle.gameObject.SetActive(true);
        if (_markAsDoneButton != null) _markAsDoneButton.gameObject.SetActive(true);
        if (_materialsButton != null) _materialsButton.gameObject.SetActive(true);

        foreach (Toggle toggle in _sharedRiskToggles)
        {
            if (toggle != null)
            {
                toggle.interactable = false;
            }
        }
    }

    public void ShowTaskDetails(Dictionary<string, object> taskData)
    {
        if (_mainTaskPanel == null)
        {
            Debug.LogError("Main Task Panel (mainTaskPanel) chưa được gán!");
            return;
        }

        _mainTaskPanel.SetActive(true);
        ShowDetailsView();

        if (_taskContentInput != null) _taskContentInput.text = "Tên công việc: " + (taskData.TryGetValue("content", out object contentVal) ? contentVal.ToString() : "N/A");
        if (_taskLocationInput != null) _taskLocationInput.text = "Vị trí: " + (taskData.TryGetValue("location", out object locationVal) ? locationVal.ToString() : "N/A");
        if (_taskDescriptionInput != null) _taskDescriptionInput.text = "Mô tả chi tiết: " + (taskData.TryGetValue("description", out object descriptionVal) ? descriptionVal.ToString() : "N/A");

        List<string> selectedRisks = new List<string>();
        if (taskData.TryGetValue("risks", out object risksObject) && risksObject is List<object> risksList)
        {
            foreach (object riskItem in risksList)
            {
                selectedRisks.Add(riskItem.ToString());
            }
        }

        for (int i = 0; i < _sharedRiskToggles.Length; i++)
        {
            if (_sharedRiskToggles[i] != null)
            {
                bool isSelected = selectedRisks.Contains(_riskNames[i]);
                _sharedRiskToggles[i].gameObject.SetActive(isSelected);
                _sharedRiskToggles[i].isOn = isSelected;
                _sharedRiskToggles[i].interactable = false;
            }
        }

        string currentStatus = taskData.TryGetValue("status", out object statusVal) ? statusVal.ToString() : TaskConstants.STATUS_PENDING;
        if (_detailsStatusToggle != null)
        {
            _detailsStatusToggle.gameObject.SetActive(true);
            _detailsStatusToggle.interactable = true;
            if (currentStatus == TaskConstants.STATUS_IN_PROGRESS)
            {
                _detailsStatusToggle.isOn = true;
            }
            else
            {
                _detailsStatusToggle.isOn = false;
            }
            if (currentStatus == TaskConstants.STATUS_DONE)
            {
                _detailsStatusToggle.interactable = false;
                if (_markAsDoneButton != null) _markAsDoneButton.interactable = false;
            }
            else
            {
                if (_markAsDoneButton != null) _markAsDoneButton.interactable = true;
            }
        }
        if (_markAsDoneButton != null) _markAsDoneButton.gameObject.SetActive(true);
        if (_materialsButton != null) _materialsButton.gameObject.SetActive(true);
    }

    public void CloseTaskDetails()
    {
        if (_mainTaskPanel != null)
        {
            _mainTaskPanel.SetActive(false);
            ShowInputView();
        }
    }

    public void ToggleTaskListPanelVisibility()
    {
        if (_taskListPanel != null)
        {
            _taskListPanel.SetActive(!_taskListPanel.activeSelf);
        }
        if (_statusFilterDropdown != null)
        {
            int pendingIndex = _statusFilterDropdown.options.FindIndex(option => option.text == TaskConstants.STATUS_PENDING);
            if (pendingIndex != -1)
            {
                _statusFilterDropdown.value = pendingIndex;
                _statusFilterDropdown.RefreshShownValue();
            }
        }
    }

    public void OnShowInputPanelButtonClicked()
    {
        if (_mainTaskPanel != null)
        {
            _mainTaskPanel.SetActive(true);
            ShowInputView();
        }
        else
        {
            Debug.LogError("Main Task Panel (mainTaskPanel) chưa được gán!");
        }
    }

    public void ShowLoadingIndicator(string messagePrefix = "Đang tải dữ liệu...")
    {
        if (_loadingIndicatorPanel != null)
        {
            _loadingIndicatorPanel.SetActive(true);
            if (_monoBehaviourContext != null)
            {
                if (_loadingCoroutine != null) _monoBehaviourContext.StopCoroutine(_loadingCoroutine);
                _loadingCoroutine = _monoBehaviourContext.StartCoroutine(SimulateLoadingProgress(2.0f, messagePrefix));
            }
        }
    }

    public void HideLoadingIndicator()
    {
        if (_loadingIndicatorPanel != null)
        {
            if (_loadingCoroutine != null && _monoBehaviourContext != null)
            {
                _monoBehaviourContext.StopCoroutine(_loadingCoroutine);
            }
            _loadingIndicatorPanel.SetActive(false);
            if (_loadingPercentageText != null) _loadingPercentageText.text = "100%";
        }
    }

    private IEnumerator SimulateLoadingProgress(float duration, string prefixText)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            int percentage = Mathf.RoundToInt(progress * 100);
            if (_loadingPercentageText != null)
            {
                _loadingPercentageText.text = $"{prefixText} {percentage}%";
            }
            yield return null;
        }

        if (_loadingPercentageText != null)
        {
            _loadingPercentageText.text = "100%";
        }
    }

    public void ShowNotification(string title, string message)
    {
        if (_notificationText != null)
        {
            _notificationText.text = $"{title}: {message}";
        }
        else
        {
            Debug.Log($"[NOTIFICATION] {title}: {message}");
        }
    }

    public string GetSelectedStatusFilter()
    {
        if (_statusFilterDropdown != null && _statusFilterDropdown.options.Count > _statusFilterDropdown.value)
        {
            return _statusFilterDropdown.options[_statusFilterDropdown.value].text;
        }
        return TaskConstants.STATUS_ALL;
    }

    // Phương thức mới để cập nhật danh sách đã lọc
    public void UpdateFilteredTasksUI(List<Dictionary<string, object>> filteredTasks)
    {
        ClearSpecificTasksUI(_tasksListParent);
        foreach (var taskData in filteredTasks)
        {
            DisplayTask(taskData, _tasksListParent, false);
        }
    }

    // Phương thức mới để cập nhật danh sách "Đang làm"
    public void UpdateInProgressTasksUI(List<Dictionary<string, object>> inProgressTasks)
    {
        ClearSpecificTasksUI(_inProgressTasksListParent);
        foreach (var taskData in inProgressTasks)
        {
            DisplayTask(taskData, _inProgressTasksListParent, false);
        }
    }

    public void ShowNewTaskNotification(string content, string location)
    {
        ShowNotification("Công việc mới!", $"'{content}' tại '{location}'");
    }

    private void ClearSpecificTasksUI(Transform parentTransform)
    {
        if (parentTransform == null)
        {
            Debug.LogError("Parent Transform chưa được gán! Không thể xóa UI công việc.");
            return;
        }

        foreach (Transform child in parentTransform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    private void DisplayTask(Dictionary<string, object> taskData, Transform parentTransform, bool isShortDisplay)
    {
        if (_taskItemPrefab == null || parentTransform == null)
        {
            Debug.LogError("Task Item Prefab hoặc Parent Transform chưa được gán!");
            return;
        }

        GameObject taskUI = GameObject.Instantiate(_taskItemPrefab, parentTransform);

        TaskItemUI taskItemUIScript = taskUI.GetComponent<TaskItemUI>();
        if (taskItemUIScript != null)
        {
            string content = taskData.TryGetValue("content", out object contentVal) ? contentVal.ToString() : "N/A";
            string location = taskData.TryGetValue("location", out object locationVal) ? locationVal.ToString() : "N/A";
            string createdBy = taskData.TryGetValue("createdBy", out object createdByVal) ? createdByVal.ToString() : "N/A";
            string status = taskData.TryGetValue("status", out object statusVal) ? statusVal.ToString() : TaskConstants.STATUS_PENDING;

            string timestampString = "Đang tải...";
            if (taskData.TryGetValue("timestamp", out object timestampObject) && timestampObject is Firebase.Firestore.Timestamp)
            {
                Firebase.Firestore.Timestamp ts = (Firebase.Firestore.Timestamp)timestampObject;
                timestampString = isShortDisplay ? ts.ToDateTime().ToString("HH:mm") : ts.ToDateTime().ToString("dd/MM/yyyy HH:mm:ss");
            }

            taskItemUIScript.SetBasicTaskData(content, location, createdBy, timestampString, status);

            if (isShortDisplay)
            {
                if (taskItemUIScript.locationText != null) taskItemUIScript.locationText.gameObject.SetActive(false);
                if (taskItemUIScript.createdByText != null) taskItemUIScript.createdByText.gameObject.SetActive(false);
            }
            else
            {
                if (taskItemUIScript.locationText != null) taskItemUIScript.locationText.gameObject.SetActive(true);
                if (taskItemUIScript.createdByText != null) taskItemUIScript.createdByText.gameObject.SetActive(true);
            }

            if (taskItemUIScript.taskItemButton != null)
            {
                var currentTaskData = taskData;
                taskItemUIScript.taskItemButton.onClick.AddListener(() => OnTaskItemClicked?.Invoke(currentTaskData));
            }
            else
            {
                Debug.LogWarning("Task Item Button (taskItemButton) not assigned on TaskItemUI prefab. Cannot show details.");
            }
        }
        else
        {
            Debug.LogWarning("TaskItemUI script không tìm thấy trên prefab. Đảm bảo bạn đã gắn nó vào prefab.");
        }
    }
    
    public void ShowConfirmPopup(string message)
    {
        if (_confirmPopupPanel != null)
        {
            _confirmPopupPanel.SetActive(true);
            if (_confirmPopupText != null)
            {
                _confirmPopupText.text = message;
            }
        }
    }

    public void HideConfirmPopup()
    {
        if (_confirmPopupPanel != null)
        {
            _confirmPopupPanel.SetActive(false);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;

public class TaskManager : MonoBehaviour
{
    [Header("UI Elements - Main Task Panel (Input & Details)")]
    public GameObject mainTaskPanel;
    public TMP_InputField taskContentInput;
    public TMP_InputField taskLocationInput;
    public TMP_InputField taskDescriptionInput;
    public Button addTaskButton;
    public Button closeDetailsButton;

    [Header("UI Elements - Task Risks (Shared Toggles)")]
    public Toggle[] sharedRiskToggles = new Toggle[13];

    [Header("UI Elements - Task List Display")]
    public Transform tasksListParent;
    public GameObject taskItemPrefab;
    public TextMeshProUGUI notificationText;

    [Header("UI Elements - Loading")]
    public GameObject loadingIndicatorPanel;
    public TextMeshProUGUI loadingPercentageText;

    [Header("UI Elements - Navigation")]
    public Button showTaskListButton;
    public GameObject taskListPanel;
    public Button showInputPanelButton;

    [Header("UI Elements - Status Management")]
    public TaskStatusController taskStatusController;

    [Header("UI Elements - Task List Filtering")]
    public TMP_Dropdown statusFilterDropdown;                
    [Header("UI Elements - Confirmation Popup")]
    public GameObject confirmPopupPanel;
    public TextMeshProUGUI confirmPopupText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    
    [Header("UI Elements - Dashboard")]
    public Transform inProgressTasksListParent;

    [Header("UI Elements - Pagination")]
    public Button loadMoreButton;

    private TaskDataHandler _taskDataHandler;
    private TaskUIManager _taskUIManager;

    private string _currentSelectedTaskId;
    private string _tempNewStatus;
    private DocumentSnapshot _lastVisibleDocument;

    void Start()
    {
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
        {
            Debug.LogError("FirebaseManager chưa được khởi tạo hoặc Firebase chưa sẵn sàng. Thử lại sau 1 giây.");
            Invoke("Start", 1f);
            return;
        }

        _taskDataHandler = new TaskDataHandler(FirebaseManager.Instance.db, FirebaseManager.Instance.GetCanvasAppId());
        _taskUIManager = new TaskUIManager(
            this,
            mainTaskPanel, taskContentInput, taskLocationInput, taskDescriptionInput,
            addTaskButton, closeDetailsButton, sharedRiskToggles, notificationText,
            loadingIndicatorPanel, loadingPercentageText,
            showTaskListButton, taskListPanel, showInputPanelButton,
            statusFilterDropdown, tasksListParent, taskItemPrefab,
            inProgressTasksListParent,
            confirmPopupPanel, confirmPopupText, confirmYesButton, confirmNoButton,
            loadMoreButton
        );

        _taskUIManager.OnAddTaskClicked += HandleAddTask;
        _taskUIManager.OnCloseDetailsClicked += HandleCloseTaskDetails;
        _taskUIManager.OnShowTaskListClicked += HandleShowTaskList;
        _taskUIManager.OnShowInputPanelClicked += HandleShowInputPanel;
        _taskUIManager.OnStatusFilterChanged += HandleStatusFilterChange;
        _taskUIManager.OnTaskItemClicked += HandleTaskItemClick;
        _taskUIManager.OnConfirmYesClicked += HandleConfirmYesClick;
        _taskUIManager.OnConfirmNoClicked += HandleConfirmNoClick;
        _taskUIManager.OnLoadMoreClicked += HandleLoadMoreTasks;

        if (taskStatusController != null)
        {
            taskStatusController.OnStatusChanged += HandleTaskStatusChanged;
        }

        _taskDataHandler.OnFilteredTasksLoaded += HandleFilteredTasksLoaded;
        _taskDataHandler.OnInProgressTasksChanged += _taskUIManager.UpdateInProgressTasksUI;
        _taskDataHandler.OnInitialLoadComplete += _taskUIManager.HideLoadingIndicator;
        _taskDataHandler.OnNewTaskAdded += _taskUIManager.ShowNewTaskNotification;

        _taskUIManager.InitializeUIState();
        _taskUIManager.InitializeSharedRiskTogglesLabels();
        
        _taskDataHandler.StartListeningForInProgressTasks();
        LoadInitialTasks();
    }

    void OnDestroy()
    {
        if (_taskUIManager != null)
        {
            _taskUIManager.OnAddTaskClicked -= HandleAddTask;
            _taskUIManager.OnCloseDetailsClicked -= HandleCloseTaskDetails;
            _taskUIManager.OnShowTaskListClicked -= HandleShowTaskList;
            _taskUIManager.OnShowInputPanelClicked -= HandleShowInputPanel;
            _taskUIManager.OnStatusFilterChanged -= HandleStatusFilterChange;
            _taskUIManager.OnTaskItemClicked -= HandleTaskItemClick;
            _taskUIManager.OnConfirmYesClicked -= HandleConfirmYesClick;
            _taskUIManager.OnConfirmNoClicked -= HandleConfirmNoClick;
            _taskUIManager.OnLoadMoreClicked -= HandleLoadMoreTasks;
        }

        if (taskStatusController != null)
        {
            taskStatusController.OnStatusChanged -= HandleTaskStatusChanged;
        }

        if (_taskDataHandler != null)
        {
            _taskDataHandler.OnFilteredTasksLoaded -= HandleFilteredTasksLoaded;
            _taskDataHandler.OnInProgressTasksChanged -= _taskUIManager.UpdateInProgressTasksUI;
            _taskDataHandler.OnInitialLoadComplete -= _taskUIManager.HideLoadingIndicator;
            _taskDataHandler.OnNewTaskAdded -= _taskUIManager.ShowNewTaskNotification;
            _taskDataHandler.StopListeningForInProgressTasks();
        }
    }
    
    private void LoadInitialTasks()
    {
        _taskUIManager.ShowLoadingIndicator("Đang tải công việc...");
        _taskUIManager.ClearFilteredTasksUI();
        _lastVisibleDocument = null;
        _taskDataHandler.LoadFilteredTasksAsync(_taskUIManager.GetSelectedStatusFilter(), _lastVisibleDocument);
    }
    
    private void HandleFilteredTasksLoaded(List<Dictionary<string, object>> tasks, bool hasMore)
    {
        _taskUIManager.HideLoadingIndicator();
        _taskUIManager.AppendFilteredTasksUI(tasks, hasMore);
        if (tasks.Count > 0)
        {
            _lastVisibleDocument = (DocumentSnapshot)tasks[tasks.Count - 1]["documentSnapshot"];
        }
    }
    
    private void HandleLoadMoreTasks()
    {
        _taskUIManager.ShowLoadingIndicator("Đang tải thêm công việc...");
        _taskDataHandler.LoadFilteredTasksAsync(_taskUIManager.GetSelectedStatusFilter(), _lastVisibleDocument);
    }

    private void HandleAddTask(string content, string location, string description, string[] selectedRisks)
    {
        _taskDataHandler.AddTask(content, location, description, selectedRisks, PlayerPrefs.GetString("UserName1", "Người dùng ẩn danh"));
        _taskUIManager.ShowInputView();
        if (mainTaskPanel != null) mainTaskPanel.SetActive(false);
    }

    private void HandleCloseTaskDetails()
    {
        _currentSelectedTaskId = null;
        _taskUIManager.CloseTaskDetails();
    }

    private void HandleShowTaskList()
    {
        _taskUIManager.ToggleTaskListPanelVisibility();
        LoadInitialTasks();
    }

    private void HandleShowInputPanel()
    {
        _taskUIManager.OnShowInputPanelButtonClicked();
    }

    private void HandleStatusFilterChange(int index)
    {
        LoadInitialTasks();
    }

    private void HandleTaskItemClick(Dictionary<string, object> taskData)
    {
        _currentSelectedTaskId = taskData.ContainsKey("id") ? taskData["id"].ToString() : null;
        _taskUIManager.ShowTaskDetails(taskData);
        string currentStatus = taskData.TryGetValue("status", out object statusVal) ? statusVal.ToString() : TaskConstants.STATUS_PENDING;
        if (taskStatusController != null)
        {
            taskStatusController.SetStatus(currentStatus);
        }
    }

    private async void HandleTaskStatusChanged(string newStatus)
    {
        _tempNewStatus = newStatus;
        _taskUIManager.ClearFilteredTasksUI();
        _taskUIManager.ShowConfirmPopup($"Bạn có chắc chắn muốn thay đổi trạng thái công việc này thành '{_tempNewStatus}' không?");
    }

    private async void HandleConfirmYesClick()
    {
        _taskUIManager.HideConfirmPopup();
        if (!string.IsNullOrEmpty(_currentSelectedTaskId) && !string.IsNullOrEmpty(_tempNewStatus))
        {
            _taskUIManager.ShowLoadingIndicator("Đang cập nhật trạng thái...");
            await _taskDataHandler.UpdateTaskStatus(_currentSelectedTaskId, _tempNewStatus);
            _taskUIManager.HideLoadingIndicator();
            _taskUIManager.ShowNotification("Thành công", $"Trạng thái công việc đã được cập nhật thành '{_tempNewStatus}'!");

            if (_tempNewStatus == TaskConstants.STATUS_DONE)
            {
                _taskUIManager.CloseTaskDetails();
            }
            
            LoadInitialTasks();
        }
        _tempNewStatus = null;
    }

    private void HandleConfirmNoClick()
    {
        _taskUIManager.HideConfirmPopup();
        if (taskStatusController != null)
        {
            taskStatusController.RevertToPreviousStatus();
        }
        _tempNewStatus = null;
    }
}

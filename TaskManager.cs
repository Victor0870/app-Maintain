using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;

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
    
    // Thêm tham chiếu mới cho danh sách công việc đang làm
    [Header("UI Elements - Dashboard")]
    public Transform inProgressTasksListParent;

    private TaskDataHandler _taskDataHandler;
    private TaskUIManager _taskUIManager;

    private string _currentSelectedTaskId;
    private string _tempNewStatus;

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
            mainTaskPanel, taskContentInput, taskLocationInput, taskDescriptionInput,
            addTaskButton, closeDetailsButton, sharedRiskToggles, notificationText,
            loadingIndicatorPanel, loadingPercentageText,
            showTaskListButton, taskListPanel, showInputPanelButton,statusFilterDropdown, tasksListParent, taskItemPrefab,
                        inProgressTasksListParent, // Tham số mới
            confirmPopupPanel, confirmPopupText, confirmYesButton, confirmNoButton
        );

        _taskUIManager.OnAddTaskClicked += HandleAddTask;
        _taskUIManager.OnCloseDetailsClicked += HandleCloseTaskDetails;
        _taskUIManager.OnShowTaskListClicked += HandleShowTaskList;
        _taskUIManager.OnShowInputPanelClicked += HandleShowInputPanel;
        _taskUIManager.OnStatusFilterChanged += HandleStatusFilterChange;
        _taskUIManager.OnTaskItemClicked += HandleTaskItemClick;
        _taskUIManager.OnConfirmYesClicked += HandleConfirmYesClick;
        _taskUIManager.OnConfirmNoClicked += HandleConfirmNoClick;

        if (taskStatusController != null)
        {
            taskStatusController.OnStatusChanged += HandleTaskStatusChanged;
        }

        _taskDataHandler.OnTasksDataChanged += _taskUIManager.UpdateAllTaskListsUI;
        _taskDataHandler.OnInitialLoadComplete += _taskUIManager.HideLoadingIndicator;
        _taskDataHandler.OnNewTaskAdded += _taskUIManager.ShowNewTaskNotification;

        _taskUIManager.InitializeUIState();
        _taskUIManager.InitializeSharedRiskTogglesLabels();
        
        // Tải danh sách công việc đang làm ngay khi bắt đầu
        _taskDataHandler.StartListeningForTasks(TaskConstants.STATUS_IN_PROGRESS);
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
        }

        if (taskStatusController != null)
        {
            taskStatusController.OnStatusChanged -= HandleTaskStatusChanged;
        }

        if (_taskDataHandler != null)
        {
            _taskDataHandler.OnTasksDataChanged -= _taskUIManager.UpdateAllTaskListsUI;
            _taskDataHandler.OnInitialLoadComplete -= _taskUIManager.HideLoadingIndicator;
            _taskDataHandler.OnNewTaskAdded -= _taskUIManager.ShowNewTaskNotification;
            _taskDataHandler.StopListeningForTasks();
        }
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
        // Tải danh sách công việc với bộ lọc mặc định là "Đang chờ"
        _taskDataHandler.StartListeningForTasks(TaskConstants.STATUS_PENDING);
        // Dashboard vẫn giữ danh sách đang làm riêng biệt
        _taskUIManager.ShowNotification("Thông báo", "Đang hiển thị danh sách công việc.");
    }

    private void HandleShowInputPanel()
    {
        _taskUIManager.OnShowInputPanelButtonClicked();
    }

    private void HandleStatusFilterChange(int index)
    {
        _taskDataHandler.StartListeningForTasks(_taskUIManager.GetSelectedStatusFilter());
        // Dashboard giữ nguyên danh sách đang làm, không bị ảnh hưởng
    }

    private void HandleTaskItemClick(Dictionary<string, object> taskData)
    {
        _currentSelectedTaskId = taskData.ContainsKey("id") ? taskData["id"].ToString() : null;
        
        // Cập nhật UI hiển thị chi tiết công việc
        _taskUIManager.ShowTaskDetails(taskData);
        
        // Cập nhật trạng thái của Toggle Group
        string currentStatus = taskData.TryGetValue("status", out object statusVal) ? statusVal.ToString() : TaskConstants.STATUS_PENDING;
        if (taskStatusController != null)
        {
            taskStatusController.SetStatus(currentStatus);
        }
    }

    private void HandleTaskStatusChanged(string newStatus)
    {
        _tempNewStatus = newStatus;
        // Hiển thị popup xác nhận với thông điệp phù hợp
        _taskUIManager.ShowConfirmPopup($"Bạn có chắc chắn muốn thay đổi trạng thái công việc này thành '{_tempNewStatus}' không?");
    }

    private void HandleConfirmYesClick()
    {
        _taskUIManager.HideConfirmPopup();
        if (!string.IsNullOrEmpty(_currentSelectedTaskId) && !string.IsNullOrEmpty(_tempNewStatus))
        {
            _taskDataHandler.UpdateTaskStatus(_currentSelectedTaskId, _tempNewStatus);
            _taskUIManager.ShowNotification("Thành công", $"Trạng thái công việc đã được cập nhật thành '{_tempNewStatus}'!");

            if (_tempNewStatus == TaskConstants.STATUS_DONE)
            {
                _taskUIManager.CloseTaskDetails();
            }
            
            // Yêu cầu TaskDataHandler tải lại cả hai danh sách sau khi cập nhật
            _taskDataHandler.StartListeningForTasks(TaskConstants.STATUS_PENDING);
        // Dashboard vẫn giữ danh sách đang làm riêng biệt
            _taskDataHandler.StartListeningForTasks(TaskConstants.STATUS_IN_PROGRESS);
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

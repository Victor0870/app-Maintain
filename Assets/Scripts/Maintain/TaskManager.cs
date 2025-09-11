using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySpace;
using System.Linq;

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
    public Button loadMoreButton;

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

    [Header("UI Elements - Risks Dynamic Generation")]
    public GameObject riskTogglePrefab;
    public Transform riskTogglesParent;

    private TaskDataHandler _taskDataHandler;
    private TaskUIManager _taskUIManager;
    private MaterialManager _materialManager;
    private FirebaseToBGDatabaseRiskSynchronizer _riskSynchronizer;
    private FirebaseToBGDatabaseTaskSynchronizer _taskSynchronizer; // Thêm biến này

    private string _currentSelectedTaskId;
    private string _tempNewStatus;

    async void Start()
    {
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
        {
            Debug.LogError("FirebaseManager chưa được khởi tạo hoặc Firebase chưa sẵn sàng. Thử lại sau 1 giây.");
            Invoke("Start", 1f);
            return;
        }

        _materialManager = FindObjectOfType<MaterialManager>();
        if (_materialManager == null)
        {
            Debug.LogError("Thiếu MaterialManager trong scene.");
            return;
        }

        _riskSynchronizer = FindObjectOfType<FirebaseToBGDatabaseRiskSynchronizer>();
        if (_riskSynchronizer == null)
        {
            Debug.LogError("Thiếu FirebaseToBGDatabaseRiskSynchronizer trong scene.");
            return;
        }

        _taskSynchronizer = FindObjectOfType<FirebaseToBGDatabaseTaskSynchronizer>(); // Tìm bộ đồng bộ hóa công việc
        if (_taskSynchronizer == null)
        {
            Debug.LogError("Thiếu FirebaseToBGDatabaseTaskSynchronizer trong scene.");
            return;
        }

        _taskUIManager = new TaskUIManager(
            this, mainTaskPanel, taskContentInput, taskLocationInput, taskDescriptionInput,
            addTaskButton, closeDetailsButton,
            riskTogglePrefab, riskTogglesParent, notificationText,
            loadingIndicatorPanel, loadingPercentageText,
            showTaskListButton, taskListPanel, showInputPanelButton,
            statusFilterDropdown, tasksListParent, taskItemPrefab,
            inProgressTasksListParent,
            confirmPopupPanel, confirmPopupText, confirmYesButton, confirmNoButton,
            loadMoreButton
        );

        _taskUIManager.InitializeUIState();

        _taskDataHandler = new TaskDataHandler(FirebaseManager.Instance.db, FirebaseManager.Instance.GetCanvasAppId());

        // Đồng bộ hóa rủi ro
        _taskUIManager.ShowLoadingIndicator("Đang tải dữ liệu rủi ro...");
        bool successRisk = await _riskSynchronizer.SynchronizeRisksFromFirebase();
        _taskUIManager.HideLoadingIndicator();
        if (!successRisk)
        {
            Debug.LogError("Không thể đồng bộ hóa dữ liệu rủi ro.");
        }

        // Đồng bộ hóa công việc
        _taskUIManager.ShowLoadingIndicator("Đang tải dữ liệu công việc...");
        bool successTask = await _taskSynchronizer.SynchronizeTasksFromFirebase();
        _taskUIManager.HideLoadingIndicator();
        if (!successTask)
        {
            Debug.LogError("Không thể đồng bộ hóa dữ liệu công việc.");
        }

        _taskUIManager.InitializeSharedRiskTogglesLabels();

        _taskUIManager.OnAddTaskClicked += HandleAddTask;
        _taskUIManager.OnCloseDetailsClicked += HandleCloseTaskDetails;
        _taskUIManager.OnShowTaskListClicked += HandleShowTaskList;
        _taskUIManager.OnShowInputPanelClicked += HandleShowInputPanel;
        _taskUIManager.OnStatusFilterChanged += HandleStatusFilterChange;
        _taskUIManager.OnTaskItemClicked += HandleTaskItemClick;
        _taskUIManager.OnConfirmYesClicked += HandleConfirmYesClick;
        _taskUIManager.OnConfirmNoClicked += HandleConfirmNoClick;
        _taskUIManager.OnLoadMoreClicked += HandleLoadMoreTasks;
        _taskUIManager.OnMaterialsClicked += HandleMaterialsClicked;

        if (taskStatusController != null)
        {
            taskStatusController.OnStatusChanged += HandleTaskStatusChanged;
        }

        _taskDataHandler.OnFilteredTasksChanged += _taskUIManager.UpdateFilteredTasksUI;
        _taskDataHandler.OnInProgressTasksChanged += _taskUIManager.UpdateInProgressTasksUI;
        _taskDataHandler.OnInitialLoadComplete += _taskUIManager.HideLoadingIndicator;
        _taskDataHandler.OnNewTaskAdded += _taskUIManager.ShowNewTaskNotification;

        // Gọi hàm để tải và hiển thị dữ liệu từ BGDatabase thay vì lắng nghe Firebase
        _taskDataHandler.LoadFilteredTasksFromLocal(TaskConstants.STATUS_ALL);
        _taskDataHandler.LoadInProgressTasksFromLocal();
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
            _taskUIManager.OnMaterialsClicked -= HandleMaterialsClicked;
        }

        if (taskStatusController != null)
        {
            taskStatusController.OnStatusChanged -= HandleTaskStatusChanged;
        }

        if (_taskDataHandler != null)
        {
            _taskDataHandler.OnFilteredTasksChanged -= _taskUIManager.UpdateFilteredTasksUI;
            _taskDataHandler.OnInProgressTasksChanged -= _taskUIManager.UpdateInProgressTasksUI;
            _taskDataHandler.OnNewTaskAdded -= _taskUIManager.ShowNewTaskNotification;
            // Dừng lắng nghe Firebase vì chúng ta sẽ sử dụng bộ nhớ cục bộ
            _taskDataHandler.StopListeningForFilteredTasks();
            _taskDataHandler.StopListeningForInProgressTasks();
        }
    }

    private void HandleAddTask(string content, string location, string description, string[] selectedRisks)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            _taskUIManager.ShowNotification("Lỗi kết nối", "Không có kết nối mạng. Vui lòng thử lại sau.");
            return;
        }
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
    }

    private void HandleShowInputPanel()
    {
        _taskUIManager.OnShowInputPanelButtonClicked();
    }

    private void HandleStatusFilterChange(int index)
    {
        _taskUIManager.ClearFilteredTasksUI();
        _taskDataHandler.LoadFilteredTasksFromLocal(_taskUIManager.GetSelectedStatusFilter());
        _taskUIManager.ShowLoadingIndicator("Đang tải dữ liệu...");
        // Không cần tải lại từ Firebase ở đây
        _taskUIManager.HideLoadingIndicator();
    }

    private void HandleLoadMoreTasks()
    {
        // Với BGDatabase, bạn không cần LoadMoreTasks vì tất cả dữ liệu đã có sẵn
        Debug.Log("Không cần tải thêm vì dữ liệu đã có sẵn cục bộ.");
    }

     public void HandleTaskItemClick(Dictionary<string, object> taskData)
        {
            _currentSelectedTaskId = taskData.ContainsKey("id") ? taskData["id"].ToString() : null;
            Debug.Log("Đã nhấp vào công việc. ID công việc: " + _currentSelectedTaskId);
            _taskUIManager.ShowTaskDetails(taskData);
            string currentStatus = taskData.TryGetValue("status", out object statusVal) ? statusVal.ToString() : TaskConstants.STATUS_PENDING;
            if (taskStatusController != null)
            {
                taskStatusController.SetStatus(currentStatus);
            }
        }

    private void HandleTaskStatusChanged(string newStatus)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            _taskUIManager.ShowNotification("Lỗi kết nối", "Không có kết nối mạng. Vui lòng thử lại sau.");
            if (taskStatusController != null)
            {
                taskStatusController.RevertToPreviousStatus();
            }
            return;
        }

        _tempNewStatus = newStatus;
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

            _taskDataHandler.LoadFilteredTasksFromLocal(_taskUIManager.GetSelectedStatusFilter());
            _taskDataHandler.LoadInProgressTasksFromLocal();
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

    public void HandleMaterialsClicked()
    {
        if (!string.IsNullOrEmpty(_currentSelectedTaskId))
        {
            Debug.Log(" 222222222222       Đã nhấp vào nút 'Vật tư'. ID công việc được truyền: " + _currentSelectedTaskId);
            _materialManager.ShowMaterialUsagePanel(_currentSelectedTaskId);
        }
        else
        {
            Debug.LogWarning("Không có công việc nào được chọn để thêm vật tư.");
        }
    }
}
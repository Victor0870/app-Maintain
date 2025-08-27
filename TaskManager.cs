using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    
    [Header("UI Elements - Task List Pagination")]
    public Button nextPageButton;
    public Button previousPageButton;
    public TextMeshProUGUI pageNumberText;

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

    private TaskDataHandler _taskDataHandler;
    private TaskUIManager _taskUIManager;

    private string _currentSelectedTaskId;
    private string _tempNewStatus;
    private const int PAGE_SIZE = 5;
    private int _currentPage = 1;
    private int _totalPages = 1;

    private List<DocumentSnapshot> _pageCursors = new List<DocumentSnapshot>();

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
            this, mainTaskPanel, taskContentInput, taskLocationInput, taskDescriptionInput,
            addTaskButton, closeDetailsButton, sharedRiskToggles, notificationText,
            loadingIndicatorPanel, loadingPercentageText,
            showTaskListButton, taskListPanel, showInputPanelButton,
            statusFilterDropdown, tasksListParent, taskItemPrefab,
            inProgressTasksListParent,
            confirmPopupPanel, confirmPopupText, confirmYesButton, confirmNoButton,
            nextPageButton, previousPageButton, pageNumberText
        );

        _taskUIManager.OnAddTaskClicked += HandleAddTask;
        _taskUIManager.OnCloseDetailsClicked += HandleCloseTaskDetails;
        _taskUIManager.OnShowTaskListClicked += HandleShowTaskList;
        _taskUIManager.OnShowInputPanelClicked += HandleShowInputPanel;
        _taskUIManager.OnStatusFilterChanged += HandleStatusFilterChange;
        _taskUIManager.OnTaskItemClicked += HandleTaskItemClick;
        _taskUIManager.OnConfirmYesClicked += HandleConfirmYesClick;
        _taskUIManager.OnConfirmNoClicked += HandleConfirmNoClick;
        _taskUIManager.OnNextPageClicked += HandleNextPageClick;
        _taskUIManager.OnPreviousPageClicked += HandlePreviousPageClick;

        if (taskStatusController != null)
        {
            taskStatusController.OnStatusChanged += HandleTaskStatusChanged;
        }

        _taskDataHandler.OnInProgressTasksChanged += _taskUIManager.UpdateInProgressTasksUI;
        _taskDataHandler.OnInitialLoadComplete += _taskUIManager.HideLoadingIndicator;
        _taskDataHandler.OnNewTaskAdded += _taskUIManager.ShowNewTaskNotification;

        _taskUIManager.InitializeUIState();
        _taskUIManager.InitializeSharedRiskTogglesLabels();

        _taskDataHandler.StartListeningForInProgressTasks();
        LoadTasks(true);
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
            _taskUIManager.OnNextPageClicked -= HandleNextPageClick;
            _taskUIManager.OnPreviousPageClicked -= HandlePreviousPageClick;
        }

        if (taskStatusController != null)
        {
            taskStatusController.OnStatusChanged -= HandleTaskStatusChanged;
        }

        if (_taskDataHandler != null)
        {
            _taskDataHandler.OnInProgressTasksChanged -= _taskUIManager.UpdateInProgressTasksUI;
            _taskDataHandler.OnInitialLoadComplete -= _taskUIManager.HideLoadingIndicator;
            _taskDataHandler.OnNewTaskAdded -= _taskUIManager.ShowNewTaskNotification;
            _taskDataHandler.StopListeningForInProgressTasks();
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
        LoadTasks(true);
    }

    private void HandleShowInputPanel()
    {
        _taskUIManager.OnShowInputPanelButtonClicked();
    }

    private void HandleStatusFilterChange(int index)
    {
        LoadTasks(true);
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

    private void HandleTaskStatusChanged(string newStatus)
    {
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
            
            LoadTasks(true);
            _taskDataHandler.StartListeningForInProgressTasks();
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

    private void HandleNextPageClick()
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            LoadTasks(false);
        }
    }

    private void HandlePreviousPageClick()
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            LoadTasks(false);
        }
    }

    private async void LoadTasks(bool isNewFilter)
    {
        _taskUIManager.ShowLoadingIndicator("Đang tải dữ liệu...");

        if (isNewFilter)
        {
            _currentPage = 1;
            _pageCursors.Clear();
            _pageCursors.Add(null);
        }
        
        string filterStatus = _taskUIManager.GetSelectedStatusFilter();

        long totalTasks = await _taskDataHandler.GetTaskCount();
        _totalPages = (int)Mathf.Ceil((float)totalTasks / PAGE_SIZE);

        DocumentSnapshot startAfterDoc = null;
        if (_currentPage > 1)
        {
            if (_currentPage - 1 < _pageCursors.Count)
            {
                startAfterDoc = _pageCursors[_currentPage - 1];
            }
            else
            {
                DocumentSnapshot previousPageCursor = _pageCursors[_currentPage - 2];
                QuerySnapshot nextPageSnapshot = await _taskDataHandler.FetchTasksPaged(PAGE_SIZE, previousPageCursor, filterStatus);
                if (nextPageSnapshot != null && nextPageSnapshot.Documents.Any())
                {
                    _pageCursors.Add(nextPageSnapshot.Documents.Last());
                    startAfterDoc = previousPageCursor;
                }
            }
        }
        
        QuerySnapshot snapshot = await _taskDataHandler.FetchTasksPaged(PAGE_SIZE, startAfterDoc, filterStatus);

        _taskUIManager.HideLoadingIndicator();
        
        if (snapshot != null)
        {
            List<Dictionary<string, object>> tasks = new List<Dictionary<string, object>>();
            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                Dictionary<string, object> taskData = doc.ToDictionary();
                taskData["id"] = doc.Id;
                tasks.Add(taskData);
            }
            _taskUIManager.UpdateFilteredTasksUI(tasks);
        }

        _taskUIManager.UpdatePaginationUI(_currentPage, _totalPages);
    }
}

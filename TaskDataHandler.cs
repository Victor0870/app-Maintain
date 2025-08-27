using Firebase.Firestore;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TaskDataHandler
{
    private readonly FirebaseFirestore _db;
    private readonly string _canvasAppId;
    private ListenerRegistration _tasksListener;
    private ListenerRegistration _inProgressTasksListener;
    private bool _initialLoadComplete = false;

    // Biến để lưu tài liệu cuối cùng của trang trước để sử dụng cho phân trang
    private DocumentSnapshot _lastVisibleDocument;

    public event Action<List<Dictionary<string, object>>> OnInProgressTasksChanged;
    public event Action<List<Dictionary<string, object>>> OnFilteredTasksChanged;
    public event Action OnInitialLoadComplete;
    public event Action<string, string> OnNewTaskAdded;

    public TaskDataHandler(FirebaseFirestore db, string canvasAppId)
    {
        _db = db;
        _canvasAppId = canvasAppId;
    }

    public async Task FetchTasksPaged(int pageSize, string filterStatus = TaskConstants.STATUS_ALL)
    {
        if (_db == null)
        {
            Debug.LogWarning("Firestore DB chưa có. Không thể tải các công việc.");
            return;
        }

        CollectionReference tasksCollectionRef = _db.Collection("artifacts")
            .Document(_canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("tasks");

        Query q = tasksCollectionRef.OrderByDescending("timestamp").Limit(pageSize);

        if (filterStatus != TaskConstants.STATUS_ALL)
        {
            q = q.WhereEqualTo("status", filterStatus);
        }

        if (_lastVisibleDocument != null)
        {
            q = q.StartAfter(_lastVisibleDocument);
        }

        try
        {
            QuerySnapshot snapshot = await q.GetSnapshotAsync();
            List<Dictionary<string, object>> fetchedTasks = new List<Dictionary<string, object>>();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Dictionary<string, object> taskData = document.ToDictionary();
                taskData["id"] = document.Id;
                fetchedTasks.Add(taskData);
            }

            if (snapshot.Documents.Any())
            {
                _lastVisibleDocument = snapshot.Documents.Last();
            }
            else
            {
                _lastVisibleDocument = null; // Không còn dữ liệu để tải nữa
            }

            OnFilteredTasksChanged?.Invoke(fetchedTasks);
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi tải dữ liệu phân trang từ Firestore: " + ex.Message);
        }
    }

    public void StartListeningForInProgressTasks()
    {
        if (_db == null || string.IsNullOrEmpty(FirebaseManager.Instance.userId))
        {
            Debug.LogWarning("Firestore DB hoặc ID người dùng chưa có. Không thể bắt đầu lắng nghe các công việc.");
            return;
        }

        StopListeningForInProgressTasks();

        CollectionReference tasksCollectionRef = _db.Collection("artifacts")
            .Document(_canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("tasks");

        Query q = tasksCollectionRef.WhereEqualTo("status", TaskConstants.STATUS_IN_PROGRESS).OrderByDescending("timestamp");

        _inProgressTasksListener = q.Listen(snapshot =>
        {
            List<Dictionary<string, object>> inProgressTasks = new List<Dictionary<string, object>>();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Dictionary<string, object> taskData = document.ToDictionary();
                taskData["id"] = document.Id;
                inProgressTasks.Add(taskData);
            }
            OnInProgressTasksChanged?.Invoke(inProgressTasks);
        });
        Debug.Log("Đã bắt đầu lắng nghe riêng cho danh sách 'Đang làm'.");
    }

    public void StopListeningForFilteredTasks()
    {
        if (_tasksListener != null)
        {
            _tasksListener.Stop();
            _tasksListener = null;
            Debug.Log("Đã dừng lắng nghe cập nhật công việc đã lọc.");
        }
    }

    public void StopListeningForInProgressTasks()
    {
        if (_inProgressTasksListener != null)
        {
            _inProgressTasksListener.Stop();
            _inProgressTasksListener = null;
            Debug.Log("Đã dừng lắng nghe cập nhật công việc 'Đang làm'.");
        }
    }

    public async void AddTask(string content, string location, string description, string[] selectedRisks, string createdBy)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(location) || string.IsNullOrEmpty(description))
        {
            Debug.LogWarning("Content, Location, and Description cannot be empty.");
            return;
        }

        if (_db == null)
        {
            Debug.LogError("Firebase not initialized in TaskDataHandler.");
            return;
        }

        Dictionary<string, object> taskData = new Dictionary<string, object>
        {
            { "content", content },
            { "location", location },
            { "description", description },
            { "timestamp", FieldValue.ServerTimestamp },
            { "createdBy", createdBy },
            { "risks", selectedRisks },
            { "status", TaskConstants.STATUS_PENDING }
        };

        try
        {
            CollectionReference tasksCollectionRef = _db.Collection("artifacts")
                .Document(_canvasAppId)
                .Collection("public")
                .Document("data")
                .Collection("tasks");

            await tasksCollectionRef.AddAsync(taskData);
            Debug.Log("Task added successfully to Firestore!");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error adding task to Firestore: " + ex.Message);
        }
    }

    public async void UpdateTaskStatus(string taskId, string newStatus)
    {
        if (_db == null || string.IsNullOrEmpty(taskId))
        {
            Debug.LogError("Firestore DB hoặc Task ID không hợp lệ để cập nhật trạng thái.");
            return;
        }

        DocumentReference taskDocRef = _db.Collection("artifacts")
            .Document(_canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("tasks")
            .Document(taskId);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "status", newStatus },
            { "lastUpdated", FieldValue.ServerTimestamp }
        };

        try
        {
            await taskDocRef.UpdateAsync(updates);
            Debug.Log($"Trạng thái công việc {taskId} đã được cập nhật thành '{newStatus}' thành công.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi cập nhật trạng thái công việc {taskId}: {ex.Message}");
        }
    }
}

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
    private bool _initialLoadComplete = false;

    public event Action<List<Dictionary<string, object>>, List<Dictionary<string, object>>, List<Dictionary<string, object>>, List<Dictionary<string, object>>> OnTasksDataChanged;
    public event Action OnInitialLoadComplete;
    public event Action<string, string> OnNewTaskAdded;

    public TaskDataHandler(FirebaseFirestore db, string canvasAppId)
    {
        _db = db;
        _canvasAppId = canvasAppId;
    }

    public void StartListeningForTasks(string filterStatus = TaskConstants.STATUS_ALL)
    {
        if (_db == null || string.IsNullOrEmpty(FirebaseManager.Instance.userId))
        {
            Debug.LogWarning("Firestore DB hoặc ID người dùng chưa có. Không thể bắt đầu lắng nghe các công việc.");
            return;
        }

        StopListeningForTasks();

        CollectionReference tasksCollectionRef = _db.Collection("artifacts")
            .Document(_canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("tasks");

        Query q = tasksCollectionRef.OrderByDescending("timestamp");

        if (filterStatus != TaskConstants.STATUS_ALL)
        {
            q = q.WhereEqualTo("status", filterStatus);
            Debug.Log($"Áp dụng bộ lọc trạng thái: {filterStatus}");
        }

        _tasksListener = q.Listen(snapshot =>
        {
            Debug.Log("Dữ liệu công việc đã được cập nhật! Số lượng tài liệu: " + snapshot.Documents.Count());

            List<Dictionary<string, object>> allTasks = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> todayTasks = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> newTasks = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> inProgressTasks = new List<Dictionary<string, object>>();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Dictionary<string, object> taskData = document.ToDictionary();
                taskData["id"] = document.Id;
                allTasks.Add(taskData);

                if (taskData.TryGetValue("timestamp", out object timestampObject) && timestampObject is Firebase.Firestore.Timestamp ts)
                {
                    DateTime taskDate = ts.ToDateTime().Date;
                    if (taskDate == DateTime.Today)
                    {
                        todayTasks.Add(taskData);
                    }
                }

                if (taskData.TryGetValue("status", out object statusVal) && statusVal.ToString() == TaskConstants.STATUS_IN_PROGRESS)
                {
                    inProgressTasks.Add(taskData);
                }
            }

            OnTasksDataChanged?.Invoke(allTasks, todayTasks, newTasks, inProgressTasks);

            if (!_initialLoadComplete)
            {
                _initialLoadComplete = true;
                OnInitialLoadComplete?.Invoke();
                Debug.Log("Đã hoàn tất tải dữ liệu ban đầu.");
            }

            if (snapshot.GetChanges().Any())
            {
                foreach (DocumentChange change in snapshot.GetChanges())
                {
                    if (change.ChangeType == DocumentChange.Type.Added)
                    {
                        Dictionary<string, object> changedDocData = change.Document.ToDictionary();
                        string content = changedDocData.ContainsKey("content") ? changedDocData["content"].ToString() : "N/A";
                        string location = changedDocData.ContainsKey("location") ? changedDocData["location"].ToString() : "N/A";
                        OnNewTaskAdded?.Invoke(content, location);
                    }
                }
            }
        });

        Debug.Log("Đã bắt đầu lắng nghe cập nhật công việc.");
    }

    public void StopListeningForTasks()
    {
        if (_tasksListener != null)
        {
            _tasksListener.Stop();
            _tasksListener = null;
            Debug.Log("Đã dừng lắng nghe cập nhật công việc.");
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

using Firebase.Firestore;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using MySpace;

public class TaskDataHandler
{
    private readonly FirebaseFirestore _db;
    private readonly string _canvasAppId;
    private ListenerRegistration _tasksListener;

    public event Action<List<Dictionary<string, object>>> OnInProgressTasksChanged;
    public event Action<List<Dictionary<string, object>>> OnFilteredTasksChanged;
    public event Action OnInitialLoadComplete;
    public event Action<string, string> OnNewTaskAdded;

    public TaskDataHandler(FirebaseFirestore db, string canvasAppId)
    {
        _db = db;
        _canvasAppId = canvasAppId;
    }

    public void StartListeningForTasks(string filterStatus)
    {
        StopListeningForTasks();

        if (_db == null || string.IsNullOrEmpty(FirebaseManager.Instance.userId))
        {
            Debug.LogWarning("Firestore DB hoặc ID người dùng chưa có. Không thể lắng nghe công việc.");
            return;
        }

        CollectionReference tasksCollectionRef = FirebasePathUtils.GetTasksCollection(_canvasAppId, _db);
        Query query = tasksCollectionRef.OrderByDescending("lastUpdated");
        if (filterStatus != TaskConstants.STATUS_ALL)
        {
            query = query.WhereEqualTo("status", filterStatus);
        }

        _tasksListener = query.Listen(snapshot =>
        {
            foreach (DocumentChange change in snapshot.GetChanges())
            {
                var doc = change.Document;
                string firestoreDocId = doc.Id;
                var localTask = E_Task.FindEntity(entity => entity.f_Id == firestoreDocId);

                switch (change.ChangeType)
                {
                    case DocumentChange.Type.Added:
                    case DocumentChange.Type.Modified:
                        var taskData = doc.ToDictionary();
                        string content = taskData.TryGetValue("content", out object contentValue) ? contentValue.ToString() : "";
                        string location = taskData.TryGetValue("location", out object locationValue) ? locationValue.ToString() : "";
                        string description = taskData.TryGetValue("description", out object descriptionValue) ? descriptionValue.ToString() : "";
                        string createdBy = taskData.TryGetValue("createdBy", out object createdByValue) ? createdByValue.ToString() : "";
                        string status = taskData.TryGetValue("status", out object statusValue) ? statusValue.ToString() : TaskConstants.STATUS_PENDING;

                        DateTime taskTimestamp = DateTime.Now;
                        if (taskData.TryGetValue("lastUpdated", out object lastUpdatedValue) && lastUpdatedValue is Timestamp firebaseTimestamp)
                        {
                            taskTimestamp = firebaseTimestamp.ToDateTime();
                        }

                        List<int> risks = new List<int>();
                        if (taskData.TryGetValue("risks", out object risksObject) && risksObject is List<object> risksList)
                        {
                            foreach (object riskItem in risksList)
                            {
                                if (riskItem is long riskIdLong)
                                {
                                    risks.Add((int)riskIdLong);
                                }
                            }
                        }

                        if (localTask == null)
                        {
                            localTask = E_Task.NewEntity();
                            localTask.f_Id = firestoreDocId;
                        }

                        localTask.f_name = content;
                        localTask.f_location = location;
                        localTask.f_description = description;
                        localTask.f_createdBy = createdBy;
                        localTask.f_status = status;
                        localTask.f_risks = risks;
                        localTask.f_lastUpdated = taskTimestamp;
                        break;

                    case DocumentChange.Type.Removed:
                        if (localTask != null)
                        {
                            localTask.Delete();
                        }
                        break;
                }
            }

            SaveData.Save();
            LoadFilteredTasksFromLocal(filterStatus);
            LoadInProgressTasksFromLocal();
        });

        Debug.Log($"Đã bắt đầu lắng nghe công việc với bộ lọc: {filterStatus}.");
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

    public void LoadFilteredTasksFromLocal(string filterStatus)
    {
        List<E_Task> tasksFromLocal;
        if (filterStatus == TaskConstants.STATUS_ALL)
        {
            tasksFromLocal = E_Task.FindEntities(entity => true).ToList();
        }
        else
        {
            tasksFromLocal = E_Task.FindEntities(entity => entity.f_status == filterStatus).ToList();
        }
        tasksFromLocal = tasksFromLocal.OrderByDescending(t => t.f_lastUpdated).ToList();
        List<Dictionary<string, object>> tasksAsDictionaries = ConvertTasksToDictionaryList(tasksFromLocal);
        OnFilteredTasksChanged?.Invoke(tasksAsDictionaries);
    }

    public void LoadInProgressTasksFromLocal()
    {
        List<E_Task> inProgressTasks = E_Task.FindEntities(entity => entity.f_status == TaskConstants.STATUS_IN_PROGRESS).ToList();
        inProgressTasks = inProgressTasks.OrderByDescending(t => t.f_lastUpdated).ToList();
        List<Dictionary<string, object>> tasksAsDictionaries = ConvertTasksToDictionaryList(inProgressTasks);
        OnInProgressTasksChanged?.Invoke(tasksAsDictionaries);
    }

    private List<Dictionary<string, object>> ConvertTasksToDictionaryList(List<E_Task> tasks)
    {
        List<Dictionary<string, object>> taskDictList = new List<Dictionary<string, object>>();
        foreach(var task in tasks)
        {
            Timestamp lastUpdatedTimestamp = Timestamp.FromDateTime(task.f_lastUpdated);

            var dict = new Dictionary<string, object>
            {
                {"id", task.f_Id},
                {"content", task.f_name},
                {"location", task.f_location},
                {"description", task.f_description},
                {"createdBy", task.f_createdBy},
                {"status", task.f_status},
                {"risks", task.f_risks?.Select(id => (object)(long)id).ToList() ?? new List<object>()},
                {"lastUpdated", lastUpdatedTimestamp}
            };
            taskDictList.Add(dict);
        }
        return taskDictList;
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

        List<int> riskIds = new List<int>();
        foreach (var riskIdString in selectedRisks)
        {
            if (int.TryParse(riskIdString, out int riskId))
            {
                riskIds.Add(riskId);
            }
        }

        E_Task newTaskEntity = E_Task.NewEntity();
        newTaskEntity.f_name = content;
        newTaskEntity.f_location = location;
        newTaskEntity.f_description = description;
        newTaskEntity.f_createdBy = createdBy;
        newTaskEntity.f_status = TaskConstants.STATUS_PENDING;
        newTaskEntity.f_risks = new List<int>(riskIds);

        newTaskEntity.f_lastUpdated = DateTime.Now;

        SaveData.Save();

        await SyncTaskToFirebase(newTaskEntity);

        OnNewTaskAdded?.Invoke(content, location);
    }

    public async Task SyncTaskToFirebase(E_Task localTask)
    {
        if (_db == null)
        {
            Debug.LogError("Firebase not initialized in TaskDataHandler.");
            return;
        }

        CollectionReference tasksCollectionRef = _db.Collection("artifacts")
            .Document(_canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("tasks");

        Timestamp lastUpdatedTimestamp = Timestamp.FromDateTime(localTask.f_lastUpdated);

        Dictionary<string, object> taskData = new Dictionary<string, object>
        {
            { "content", localTask.f_name },
            { "location", localTask.f_location },
            { "description", localTask.f_description },
            { "timestamp", lastUpdatedTimestamp },
            { "createdBy", localTask.f_createdBy },
            { "risks", localTask.f_risks?.Select(id => (long)id).ToList() ?? new List<long>() },
            { "status", localTask.f_status },
            { "lastUpdated", lastUpdatedTimestamp }
        };

        try
        {
            DocumentReference newDocRef = await tasksCollectionRef.AddAsync(taskData);

            localTask.f_Id = newDocRef.Id;
            SaveData.Save();

            Debug.Log($"Task added successfully to Firestore with ID: {newDocRef.Id}");
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

        var localTask = E_Task.FindEntity(entity => entity.f_Id == taskId);
        if (localTask != null)
        {
            localTask.f_status = newStatus;
            localTask.f_lastUpdated = DateTime.Now;
            SaveData.Save();
        }

        DocumentReference taskDocRef = _db.Collection("artifacts")
            .Document(_canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("tasks")
            .Document(taskId);

        Timestamp lastUpdatedTimestamp = Timestamp.FromDateTime(DateTime.Now);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "status", newStatus },
            { "lastUpdated", lastUpdatedTimestamp }
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
    public void LoadMoreTasks()
    {
        // Khi sử dụng BGDatabase, không cần hàm này vì tất cả dữ liệu đã được tải.
    }

    public void StopListeningForFilteredTasks() {}
    public void StopListeningForInProgressTasks() {}
}
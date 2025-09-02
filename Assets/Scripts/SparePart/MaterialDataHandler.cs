using Firebase.Firestore;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MaterialDataHandler
{
    private readonly FirebaseFirestore _db;
    private readonly string _canvasAppId;
    private ListenerRegistration _materialsListener;

    public event Action<List<Dictionary<string, object>>> OnTaskMaterialsChanged;
    public List<Dictionary<string, object>> CurrentTaskMaterials { get; private set; } = new List<Dictionary<string, object>>();

    public MaterialDataHandler(FirebaseFirestore db, string canvasAppId)
    {
        _db = db;
        _canvasAppId = canvasAppId;
    }

    public void StartListeningForTaskMaterials(string taskId)
    {
        if (_db == null || string.IsNullOrEmpty(FirebaseManager.Instance.userId) || string.IsNullOrEmpty(taskId))
        {
            Debug.LogWarning("Firestore DB, ID người dùng hoặc Task ID chưa có. Không thể lắng nghe vật tư.");
            return;
        }

        StopListeningForTaskMaterials();

        _materialsListener = FirebasePathUtils.GetTaskMaterialsCollection(_canvasAppId, _db, taskId).Listen(snapshot =>
        {
            List<Dictionary<string, object>> materials = new List<Dictionary<string, object>>();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Dictionary<string, object> materialData = document.ToDictionary();
                materialData["id"] = document.Id;
                materials.Add(materialData);
            }
            CurrentTaskMaterials = materials;
            OnTaskMaterialsChanged?.Invoke(materials);
        });
        Debug.Log($"Đã bắt đầu lắng nghe danh sách vật tư cho công việc {taskId}.");
    }

    public void StopListeningForTaskMaterials()
    {
        if (_materialsListener != null)
        {
            _materialsListener.Stop();
            _materialsListener = null;
            Debug.Log("Đã dừng lắng nghe cập nhật vật tư.");
        }
    }

    public async Task AddMaterialToTask(string taskId, string materialId, int initialQuantity)
    {
        if (string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(materialId))
        {
            Debug.LogError("Task ID hoặc Material ID không hợp lệ.");
            return;
        }

        var newUsageItem = new Dictionary<string, object>
        {
            { "materialId", materialId },
            { "quantity", initialQuantity },
            { "timestamp", FieldValue.ServerTimestamp },
            { "status", "Đang chờ xác nhận" }
        };

        try
        {
            await FirebasePathUtils.GetTaskMaterialsCollection(_canvasAppId, _db, taskId).AddAsync(newUsageItem);
            Debug.Log($"Đã thêm vật tư {materialId} vào công việc {taskId}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi thêm vật tư vào công việc: {ex.Message}");
        }
    }

    public async Task UpdateMaterialUsage(string taskId, string materialId, int newQuantity, MaterialUIManager.ChangeType changeType)
    {
        if (string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(materialId))
        {
            Debug.LogError("Task ID hoặc Material ID không hợp lệ.");
            return;
        }

        try
        {
            var taskMaterialDocRef = FirebasePathUtils.GetTaskMaterialsCollection(_canvasAppId, _db, taskId)
                                                     .Document(materialId);
            var updates = new Dictionary<string, object>
            {
                { "quantity", newQuantity },
                { "lastUpdated", FieldValue.ServerTimestamp }
            };
            await taskMaterialDocRef.UpdateAsync(updates);
            Debug.Log($"Đã cập nhật số lượng vật tư {materialId} trong công việc {taskId}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi cập nhật vật tư: {ex.Message}");
        }
    }

    public async Task UpdateMaterialStock(string taskId, string materialId, int quantityChange)
    {
        if (!int.TryParse(materialId, out int materialNo))
        {
            Debug.LogError($"Không thể chuyển đổi materialId '{materialId}' thành số nguyên.");
            return;
        }

        var materialDoc = (await FirebasePathUtils.GetMaterialsCollection(_canvasAppId, _db)
                            .WhereEqualTo("No", materialNo).Limit(1).GetSnapshotAsync())
                            .Documents.FirstOrDefault();

        if (materialDoc != null)
        {
            var materialDocRef = materialDoc.Reference;
            var stockUpdates = new Dictionary<string, object>
            {
                { "stock", FieldValue.Increment(-quantityChange) }
            };

            await materialDocRef.UpdateAsync(stockUpdates);

            var usageHistory = new Dictionary<string, object>
            {
                {"taskId", taskId},
                {"quantity", quantityChange},
                {"timestamp", FieldValue.ServerTimestamp}
            };
            await materialDocRef.Collection("usageHistory").AddAsync(usageHistory);
        }
    }

}
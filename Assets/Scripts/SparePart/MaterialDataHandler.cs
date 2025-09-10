using Firebase.Firestore;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using MySpace;

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

    public async Task<string> AddMaterialToTask(string taskId, string materialId, int initialQuantity)
    {
        if (string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(materialId))
        {
            Debug.LogError("Task ID hoặc Material ID không hợp lệ.");
            return null;
        }

        var newUsageItem = new Dictionary<string, object>
        {
            { "materialId", materialId },
            { "quantity", initialQuantity },
            { "timestamp", FieldValue.ServerTimestamp },
            { "status", "Đã sử dụng" }
        };

        try
        {
            DocumentReference newDocRef = await FirebasePathUtils.GetTaskMaterialsCollection(_canvasAppId, _db, taskId).AddAsync(newUsageItem);
            Debug.Log($"Đã thêm vật tư {materialId} vào công việc {taskId}. ID Firestore: {newDocRef.Id}");
            return newDocRef.Id;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi thêm vật tư vào công việc: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateMaterialUsage(string taskId, string firestoreDocId, int newQuantity)
    {
        if (string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(firestoreDocId))
        {
            Debug.LogError("Task ID hoặc Firestore Doc ID không hợp lệ.");
            return;
        }

        try
        {
            var taskMaterialDocRef = FirebasePathUtils.GetTaskMaterialsCollection(_canvasAppId, _db, taskId)
                                                     .Document(firestoreDocId);
            var updates = new Dictionary<string, object>
            {
                { "quantity", newQuantity },
                { "timestamp", FieldValue.ServerTimestamp }
            };
            await taskMaterialDocRef.UpdateAsync(updates);
            Debug.Log($"Đã cập nhật số lượng vật tư {firestoreDocId} trong công việc {taskId}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi cập nhật vật tư: {ex.Message}");
        }
    }

    public async Task DeleteMaterialUsage(string taskId, string firestoreDocId)
    {
        if (string.IsNullOrEmpty(taskId) || string.IsNullOrEmpty(firestoreDocId))
        {
            Debug.LogError("Task ID hoặc Firestore Doc ID không hợp lệ.");
            return;
        }

        try
        {
            var taskMaterialDocRef = FirebasePathUtils.GetTaskMaterialsCollection(_canvasAppId, _db, taskId)
                                                     .Document(firestoreDocId);
            await taskMaterialDocRef.DeleteAsync();
            Debug.Log($"Đã xóa vật tư {firestoreDocId} khỏi công việc {taskId} trên Firestore.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi xóa vật tư: {ex.Message}");
        }
    }

    public async Task<string> AddPurchaseRecordToFirebase(string materialId, int quantity, string supplier, string poNumber, int price)
    {
        var newPurchaseItem = new Dictionary<string, object>
        {
            { "materialId", materialId },
            { "quantity", quantity },
            { "supplier", supplier },
            { "poNumber", poNumber },
            { "timestamp", FieldValue.ServerTimestamp },
            { "price", price }
        };

        try
        {
            DocumentReference newDocRef = await FirebasePathUtils.GetPurchasesCollection(_canvasAppId, _db).AddAsync(newPurchaseItem);
            Debug.Log($"Đã thêm bản ghi mua {quantity} vật tư {materialId} với giá {price} vào Firestore. ID: {newDocRef.Id}");
            return newDocRef.Id;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi thêm bản ghi mua vật tư: {ex.Message}");
            return null;
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
    
    public async Task AddNewMaterialToFirebase(E_SparePart newMaterial)
    {
        if (_db == null)
        {
            Debug.LogError("Firebase không được khởi tạo.");
            return;
        }

        var materialsCollection = FirebasePathUtils.GetMaterialsCollection(_canvasAppId, _db);
        
        var newMaterialData = new Dictionary<string, object>
        {
            { "No", newMaterial.f_No.ToString() },
            { "name", newMaterial.f_name },
            { "purpose", newMaterial.f_Purpose },
            { "type", newMaterial.f_Type },
            { "unit", newMaterial.f_Unit },
            { "stock", newMaterial.f_Stock },
            { "location", newMaterial.f_Location },
            { "category", newMaterial.f_Category },
            { "lastUpdated", FieldValue.ServerTimestamp }
        };

        try
        {
            DocumentReference newDocRef = await materialsCollection.AddAsync(newMaterialData);
            newMaterial.f_materialID = newDocRef.Id;
            SaveData.Save();
            Debug.Log($"Đã thêm vật tư mới '{newMaterial.f_name}' vào Firestore với ID: {newDocRef.Id}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi thêm vật tư mới vào Firestore: {ex.Message}");
        }
    }
}

using UnityEngine;
using Firebase.Firestore;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace MySpace
{
    public class FirebaseToBGDatabaseMaterialUsageSynchronizer : MonoBehaviour
    {
        public async Task<bool> SynchronizeMaterialUsagesFromFirebase(DateTime lastSyncTimestamp)
        {
            Debug.Log("Đang đồng bộ hóa lịch sử sử dụng vật tư từ Firebase xuống BGDatabase...");
            if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
            {
                Debug.LogError("Firebase không sẵn sàng. Không thể đồng bộ hóa lịch sử sử dụng.");
                return false;
            }

            try
            {
                // Truy vấn Firebase để lấy tất cả các tài liệu 'tasks'
                QuerySnapshot tasksSnapshot = await FirebasePathUtils.GetTasksCollection(FirebaseManager.Instance.GetCanvasAppId(), FirebaseManager.Instance.db).GetSnapshotAsync();

                foreach (DocumentSnapshot taskDoc in tasksSnapshot.Documents)
                {
                    string taskId = taskDoc.Id;
                    CollectionReference materialsCollection = FirebasePathUtils.GetTaskMaterialsCollection(FirebaseManager.Instance.GetCanvasAppId(), FirebaseManager.Instance.db, taskId);

                    // Lấy các bản ghi vật tư mới hơn dấu thời gian đồng bộ cuối cùng
                    Query newMaterialsQuery = materialsCollection.WhereGreaterThan("timestamp", Timestamp.FromDateTime(lastSyncTimestamp.ToUniversalTime()));
                    QuerySnapshot newMaterialsSnapshot = await newMaterialsQuery.GetSnapshotAsync();

                    foreach (DocumentSnapshot materialDoc in newMaterialsSnapshot.Documents)
                    {
                        var materialData = materialDoc.ToDictionary();
                        string firestoreDocId = materialDoc.Id;
                        string materialId = materialData.TryGetValue("materialId", out object idVal) ? idVal.ToString() : null;
                        int quantity = materialData.TryGetValue("quantity", out object quantityVal) ? (int)(long)quantityVal : 0;
                        DateTime timestamp = materialData.TryGetValue("timestamp", out object tsVal) ? ((Timestamp)tsVal).ToDateTime() : DateTime.MinValue;

                        if (materialId != null)
                        {
                            // Kiểm tra xem bản ghi đã tồn tại cục bộ chưa
                            var localUsage = E_UsageHistory.FindEntity(e => e.f_firestoreDocId == firestoreDocId);
                            if (localUsage != null)
                            {
                                // Cập nhật nếu đã tồn tại
                                localUsage.f_quantity = quantity;
                                localUsage.f_timestamp = timestamp;
                                Debug.Log($"Đã cập nhật lịch sử sử dụng cho công việc {taskId}: vật tư {materialId}.");
                            }
                            else
                            {
                                // Thêm bản ghi mới
                                var newUsageEntity = E_UsageHistory.NewEntity();
                                newUsageEntity.f_taskId = taskId;
                                newUsageEntity.f_materialId = materialId;
                                newUsageEntity.f_quantity = quantity;
                                newUsageEntity.f_timestamp = timestamp;
                                newUsageEntity.f_firestoreDocId = firestoreDocId;
                                Debug.Log($"Đã thêm mới lịch sử sử dụng cho công việc {taskId}: vật tư {materialId}.");
                            }
                        }
                    }
                }

                SaveData.Save();
                Debug.Log("Quá trình đồng bộ hóa lịch sử sử dụng vật tư đã hoàn tất.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lỗi khi đồng bộ hóa lịch sử sử dụng vật tư: {ex.Message}");
                return false;
            }
        }
    }
}
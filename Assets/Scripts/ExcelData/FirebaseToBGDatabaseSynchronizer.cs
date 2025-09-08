using UnityEngine;
using Firebase.Firestore;
using BansheeGz.BGDatabase;
using MySpace;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

public class FirebaseToBGDatabaseSynchronizer : MonoBehaviour
{
    // Thêm tham số lastSyncTimestamp để chỉ tải dữ liệu mới
    public async Task<bool> SynchronizeSparePartsFromFirebase(DateTime lastSyncTimestamp)
    {
        Debug.Log("Đang đồng bộ hóa dữ liệu từ Firebase xuống BGDatabase...");
        Debug.Log($"Thời gian đồng bộ cuối cùng cục bộ: {lastSyncTimestamp}");

        if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
        {
            Debug.LogError("Firebase không sẵn sàng. Không thể đồng bộ hóa.");
            return false;
        }

        CollectionReference materialsCollection = FirebaseManager.Instance.db
            .Collection("artifacts")
            .Document(FirebaseManager.Instance.GetCanvasAppId())
            .Collection("public")
            .Document("data")
            .Collection("materials");

        try
        {
            // Thay đổi truy vấn để chỉ lấy các tài liệu có dấu thời gian mới hơn
            Query query = materialsCollection.WhereGreaterThan("lastUpdated", lastSyncTimestamp);
            QuerySnapshot firebaseSnapshot = await query.GetSnapshotAsync();

            var firebaseDataByNo = new Dictionary<string, (Dictionary<string, object> data, string docId)>();
            foreach (DocumentSnapshot doc in firebaseSnapshot.Documents)
            {
                if (doc.TryGetValue("No", out object noValue) && noValue is string noString)
                {
                    firebaseDataByNo[noString] = (doc.ToDictionary(), doc.Id);
                }
            }

            // Lấy tất cả các entity cục bộ để so sánh
            var localEntitiesByNo = new Dictionary<string, E_SparePart>();
            E_SparePart.ForEachEntity(entity =>
            {
                localEntitiesByNo[entity.f_No.ToString()] = entity;
            });

            foreach (var firebaseItemEntry in firebaseDataByNo.Values)
            {
                var firebaseItem = firebaseItemEntry.data;
                var firebaseDocId = firebaseItemEntry.docId;
                string itemNo = firebaseItem.TryGetValue("No", out object noVal) ? (noVal?.ToString() ?? "") : "";

                if (localEntitiesByNo.ContainsKey(itemNo))
                {
                    var localEntity = localEntitiesByNo[itemNo];
                    bool needsUpdate = false;

                    // Cập nhật trường f_materialID
                    if (!string.Equals(localEntity.f_materialID, firebaseDocId)) { localEntity.f_materialID = firebaseDocId; needsUpdate = true; }

                    // Cập nhật trường f_lastUpdate
                    if (firebaseItem.TryGetValue("lastUpdated", out object lastUpdatedObj) && lastUpdatedObj is Timestamp firebaseTimestamp)
                    {
                        DateTime newTimestamp = firebaseTimestamp.ToDateTime();
                        if (localEntity.f_lastUpdate != newTimestamp)
                        {
                            localEntity.f_lastUpdate = newTimestamp;
                            needsUpdate = true;
                        }
                    }

                    // ... (Các đoạn mã so sánh và cập nhật khác)
                    string name = firebaseItem.TryGetValue("name", out object nameVal) ? (nameVal?.ToString() ?? "") : "";
                    if (!string.Equals(localEntity.f_name, name)) { localEntity.f_name = name; needsUpdate = true; }

                    string purpose = firebaseItem.TryGetValue("purpose", out object purposeVal) ? (purposeVal?.ToString() ?? "") : "";
                    if (!string.Equals(localEntity.f_Purpose, purpose)) { localEntity.f_Purpose = purpose; needsUpdate = true; }

                    string type = firebaseItem.TryGetValue("type", out object typeVal) ? (typeVal?.ToString() ?? "") : "";
                    if (!string.Equals(localEntity.f_Type, type)) { localEntity.f_Type = type; needsUpdate = true; }

                    string unit = firebaseItem.TryGetValue("unit", out object unitVal) ? (unitVal?.ToString() ?? "") : "";
                    if (!string.Equals(localEntity.f_Unit, unit)) { localEntity.f_Unit = unit; needsUpdate = true; }

                    int stock = firebaseItem.TryGetValue("stock", out object stockVal) ? (int.TryParse(stockVal?.ToString(), out int s) ? s : 0) : 0;
                    if (localEntity.f_Stock != stock) { localEntity.f_Stock = stock; needsUpdate = true; }

                    string location = firebaseItem.TryGetValue("location", out object locationVal) ? (locationVal?.ToString() ?? "") : "";
                    if (!string.Equals(localEntity.f_Location, location)) { localEntity.f_Location = location; needsUpdate = true; }

                    string category = firebaseItem.TryGetValue("category", out object categoryVal) ? (categoryVal?.ToString() ?? "") : "";
                    if (!string.Equals(localEntity.f_Category, category)) { localEntity.f_Category = category; needsUpdate = true; }

                    if (needsUpdate)
                    {
                        Debug.Log($"Đã cập nhật mục '{name}' (No: {itemNo}) trong BGDatabase.");
                    }
                }
                else
                {
                    E_SparePart newEntity = E_SparePart.NewEntity();
                    newEntity.f_No = int.Parse(itemNo);
                    newEntity.f_materialID = firebaseDocId;

                    // Thêm trường f_lastUpdate khi tạo mới
                    if (firebaseItem.TryGetValue("lastUpdated", out object lastUpdatedObj) && lastUpdatedObj is Timestamp firebaseTimestamp)
                    {
                        newEntity.f_lastUpdate = firebaseTimestamp.ToDateTime();
                    }
                    else
                    {
                        newEntity.f_lastUpdate = DateTime.MinValue;
                    }

                    newEntity.f_name = firebaseItem.TryGetValue("name", out object nameVal) ? (nameVal?.ToString() ?? "") : "";
                    newEntity.f_Purpose = firebaseItem.TryGetValue("purpose", out object purposeVal) ? (purposeVal?.ToString() ?? "") : "";
                    newEntity.f_Type = firebaseItem.TryGetValue("type", out object typeVal) ? (typeVal?.ToString() ?? "") : "";
                    newEntity.f_Unit = firebaseItem.TryGetValue("unit", out object unitVal) ? (unitVal?.ToString() ?? "") : "";
                    newEntity.f_Stock = firebaseItem.TryGetValue("stock", out object stockVal) ? (int.TryParse(stockVal?.ToString(), out int s) ? s : 0) : 0;
                    newEntity.f_Location = firebaseItem.TryGetValue("location", out object locationVal) ? (locationVal?.ToString() ?? "") : "";
                    newEntity.f_Category = firebaseItem.TryGetValue("category", out object categoryVal) ? (categoryVal?.ToString() ?? "") : "";
                    Debug.Log($"Đã thêm mới mục '{newEntity.f_name}' (No: {itemNo}) vào BGDatabase.");
                }
            }
            Debug.Log("Quá trình đồng bộ hóa đã hoàn tất.");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Lỗi khi đồng bộ hóa dữ liệu: {ex.Message}");
            return false;
        }
    }
}
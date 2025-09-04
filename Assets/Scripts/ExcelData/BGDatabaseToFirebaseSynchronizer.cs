using UnityEngine;
using Firebase.Firestore;
using BansheeGz.BGDatabase;
using MySpace;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class BGDatabaseToFirebaseSynchronizer : MonoBehaviour
{
    public async Task SynchronizeSingleSparePart(E_SparePart localItem)
       {
           Debug.Log($"Đang bắt đầu đồng bộ hóa vật tư {localItem.f_name} (No: {localItem.f_No}) lên Firebase...");

           if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
           {
               Debug.LogError("Firebase không sẵn sàng. Vui lòng kiểm tra FirebaseManager.");
               return;
           }

           if (string.IsNullOrEmpty(localItem.f_materialID))
           {
               Debug.LogError("Không có Document ID của Firebase. Không thể đồng bộ hóa.");
               return;
           }

           // 1. Lấy tham chiếu đến tài liệu trên Firebase bằng Document ID
           CollectionReference materialsCollection = FirebaseManager.Instance.db
               .Collection("artifacts")
               .Document(FirebaseManager.Instance.GetCanvasAppId())
               .Collection("public")
               .Document("data")
               .Collection("materials");

           var firebaseDocRef = materialsCollection.Document(localItem.f_materialID);

           // 2. Cập nhật các trường dữ liệu trên Firebase
           var updates = new Dictionary<string, object>
           {
               { "No", localItem.f_No.ToString() }, // Lưu dưới dạng chuỗi
               { "name", localItem.f_name },
               { "purpose", localItem.f_Purpose },
               { "type", localItem.f_Type },
               { "unit", localItem.f_Unit },
               { "stock", localItem.f_Stock },
               { "location", localItem.f_Location },
               { "category", localItem.f_Category },
               { "lastUpdated", FieldValue.ServerTimestamp }
           };

           try
           {
               await firebaseDocRef.UpdateAsync(updates);
               Debug.Log($"Đã cập nhật thành công mục '{localItem.f_name}' (ID: {localItem.f_materialID}) trên Firebase.");
           }
           catch (System.Exception ex)
           {
               Debug.LogError($"Lỗi khi cập nhật tài liệu {localItem.f_materialID}: {ex.Message}");
           }

           Debug.Log("Quá trình đồng bộ hóa đã hoàn tất.");
       }
    public async void SynchronizeSparePartsData()
    {
        Debug.Log("Đang bắt đầu đồng bộ hóa dữ liệu từ BGDatabase lên Firebase...");

        // 1. Kiểm tra kết nối Firebase
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
        {
            Debug.LogError("Firebase không sẵn sàng. Vui lòng kiểm tra FirebaseManager.");
            return;
        }

        // 2. Lấy dữ liệu từ BGDatabase
        var localData = new List<E_SparePart>();
        E_SparePart.ForEachEntity(entity => localData.Add(entity));

        if (localData.Count == 0)
        {
            Debug.LogWarning("Không tìm thấy dữ liệu vật tư trong BGDatabase. Vui lòng kiểm tra bảng 'SparePart'.");
            return;
        }

        // 3. Lấy tất cả dữ liệu hiện có trên Firebase
        CollectionReference materialsCollection = FirebaseManager.Instance.db
            .Collection("artifacts")
            .Document(FirebaseManager.Instance.GetCanvasAppId())
            .Collection("public")
            .Document("data")
            .Collection("materials");

        QuerySnapshot firebaseSnapshot = await materialsCollection.GetSnapshotAsync();
        var firebaseDocsByNo = new Dictionary<string, DocumentSnapshot>();
        foreach (DocumentSnapshot doc in firebaseSnapshot.Documents)
        {
            if (doc.TryGetValue("No", out object noValue) && noValue is string noString)
            {
                firebaseDocsByNo[noString] = doc;
            }
        }

        // 4. So sánh và đồng bộ hóa
        foreach (var localItem in localData)
        {
            // BGDatabase lưu 'No' dưới dạng số nguyên, nhưng Firebase lưu dưới dạng chuỗi (theo CSV). Cần chuyển đổi.
            string itemNo = localItem.f_No.ToString();

            if (firebaseDocsByNo.ContainsKey(itemNo))
            {
                // Mục đã tồn tại, kiểm tra và cập nhật nếu có thay đổi
                var firebaseDoc = firebaseDocsByNo[itemNo];
                var firebaseItem = firebaseDoc.ToDictionary();
                bool needsUpdate = false;

                // So sánh từng trường
                if (!string.Equals(firebaseItem["name"].ToString(), localItem.f_name)) needsUpdate = true;
                if (!string.Equals(firebaseItem["purpose"].ToString(), localItem.f_Purpose)) needsUpdate = true;
                if (!string.Equals(firebaseItem["type"].ToString(), localItem.f_Type)) needsUpdate = true;
                if (!string.Equals(firebaseItem["unit"].ToString(), localItem.f_Unit)) needsUpdate = true;
                // So sánh số nguyên
                if (int.Parse(firebaseItem["stock"].ToString()) != localItem.f_Stock) needsUpdate = true;
                if (!string.Equals(firebaseItem["location"].ToString(), localItem.f_Location)) needsUpdate = true;
                if (!string.Equals(firebaseItem["category"].ToString(), localItem.f_Category)) needsUpdate = true;

                if (needsUpdate)
                {
                    var updates = new Dictionary<string, object>
                    {
                        { "name", localItem.f_name },
                        { "purpose", localItem.f_Purpose },
                        { "type", localItem.f_Type },
                        { "unit", localItem.f_Unit },
                        { "stock", localItem.f_Stock },
                        { "location", localItem.f_Location },
                        { "category", localItem.f_Category },
                        { "lastUpdated", FieldValue.ServerTimestamp }
                    };
                    await firebaseDoc.Reference.UpdateAsync(updates);
                    Debug.Log($"Đã cập nhật mục '{localItem.f_name}' (No: {itemNo}) trên Firebase.");
                }
                else
                {
                    Debug.Log($"Mục '{localItem.f_name}' (No: {itemNo}) đã đồng bộ. Bỏ qua.");
                }
            }
            else
            {
                // Mục chưa tồn tại, thêm mới
                var newItem = new Dictionary<string, object>
                {
                    { "No", itemNo },
                    { "name", localItem.f_name },
                    { "purpose", localItem.f_Purpose },
                    { "type", localItem.f_Type },
                    { "unit", localItem.f_Unit },
                    { "stock", localItem.f_Stock },
                    { "location", localItem.f_Location },
                    { "category", localItem.f_Category },
                    { "lastUpdated", FieldValue.ServerTimestamp }
                };
                await materialsCollection.AddAsync(newItem);
                Debug.Log($"Đã thêm mới mục '{localItem.f_name}' (No: {itemNo}) vào Firebase.");
            }
        }

        Debug.Log("Quá trình đồng bộ hóa đã hoàn tất.");
    }
}
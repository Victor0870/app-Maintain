using UnityEngine;
using Firebase.Firestore;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

// Đảm bảo bạn đã thêm trường 'f_lastUpdated' kiểu 'DateTime' vào bảng E_Risk trong BGDatabase
namespace MySpace
{
    public class FirebaseToBGDatabaseRiskSynchronizer : MonoBehaviour
    {
        public async Task<bool> SynchronizeRisksFromFirebase()
        {
            Debug.Log("Đang đồng bộ hóa dữ liệu rủi ro từ Firebase xuống BGDatabase...");

            if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
            {
                Debug.LogError("Firebase không sẵn sàng. Không thể đồng bộ hóa rủi ro.");
                return false;
            }

            // Sửa đổi đường dẫn để khớp với vị trí và tên "Risk"
            CollectionReference risksCollection = FirebaseManager.Instance.db
                .Collection("artifacts")
                .Document(FirebaseManager.Instance.GetCanvasAppId())
                .Collection("public")
                .Document("data")
                .Collection("Risk");

            try
            {
                DateTime latestLocalTimestamp = E_Risk.CountEntities > 0
                    ? E_Risk.FindEntities(entity => entity.f_lastUpdated > DateTime.MinValue)
                            .OrderByDescending(entity => entity.f_lastUpdated)
                            .FirstOrDefault()?.f_lastUpdated ?? DateTime.MinValue
                    : DateTime.MinValue;

                // Thêm debug để hiển thị giá trị đang được sử dụng để so sánh
                Debug.Log($"Giá trị local để so sánh: {latestLocalTimestamp}");
                Debug.Log($"Giá trị DateTime.MinValue: {DateTime.MinValue}");

                Debug.Log($"Thời gian đồng bộ rủi ro cuối cùng cục bộ: {latestLocalTimestamp}");

                // Sửa tên trường ở đây
                Query query = risksCollection.WhereGreaterThan("lastUpdate", latestLocalTimestamp);

                QuerySnapshot firebaseSnapshot = await query.GetSnapshotAsync();

                if (firebaseSnapshot.Documents.Count() == 0)
                {
                    Debug.Log("Không có rủi ro mới để đồng bộ hóa. Quá trình hoàn tất.");
                    return true;
                }

                foreach (DocumentSnapshot doc in firebaseSnapshot.Documents)
                {
                    var riskData = doc.ToDictionary();

                    if (doc.TryGetValue("id", out object idValue) && doc.TryGetValue("name", out object nameValue)
                        // Sửa tên trường ở đây
                        && doc.TryGetValue("lastUpdate", out object lastUpdatedValue) && lastUpdatedValue is Timestamp firebaseTimestamp)
                    {
                        if (idValue is long idLong && nameValue is string nameString)
                        {
                            int riskId = (int)idLong;
                            DateTime riskTimestamp = firebaseTimestamp.ToDateTime();

                            var localRisk = E_Risk.FindEntity(entity => entity.f_Id == riskId);
                            if (localRisk != null)
                            {
                                localRisk.f_name = nameString;
                                localRisk.f_lastUpdated = riskTimestamp;
                                Debug.Log($"Đã cập nhật rủi ro '{nameString}'.");
                            }
                            else
                            {
                                E_Risk newEntity = E_Risk.NewEntity();
                                newEntity.f_Id = riskId;
                                newEntity.f_name = nameString;
                                newEntity.f_lastUpdated = riskTimestamp;
                                Debug.Log($"Đã thêm rủi ro mới '{nameString}'.");
                            }
                        }
                    }
                }

                SaveData.Save();

                Debug.Log("Quá trình đồng bộ hóa rủi ro đã hoàn tất.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lỗi khi đồng bộ hóa dữ liệu rủi ro: {ex.Message}");
                return false;
            }
        }
    }
}
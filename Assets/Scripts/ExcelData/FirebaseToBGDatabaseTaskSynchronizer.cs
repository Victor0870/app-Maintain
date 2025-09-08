using UnityEngine;
using Firebase.Firestore;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace MySpace
{
    public class FirebaseToBGDatabaseTaskSynchronizer : MonoBehaviour
    {
        public async Task<bool> SynchronizeTasksFromFirebase()
        {
            Debug.Log("Đang đồng bộ hóa dữ liệu công việc từ Firebase xuống BGDatabase...");

            if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
            {
                Debug.LogError("Firebase không sẵn sàng. Không thể đồng bộ hóa công việc.");
                return false;
            }

            CollectionReference tasksCollection = FirebaseManager.Instance.db
                .Collection("artifacts")
                .Document(FirebaseManager.Instance.GetCanvasAppId())
                .Collection("public")
                .Document("data")
                .Collection("tasks");

            try
            {
                DateTime latestLocalTimestamp = E_Task.CountEntities > 0
                    ? E_Task.FindEntities(entity => entity.f_lastUpdated > DateTime.MinValue)
                            .OrderByDescending(entity => entity.f_lastUpdated)
                            .FirstOrDefault()?.f_lastUpdated ?? DateTime.MinValue
                    : DateTime.MinValue;

                Debug.Log($"Thời gian đồng bộ công việc cuối cùng cục bộ: {latestLocalTimestamp}");

                Query query = tasksCollection.WhereGreaterThan("lastUpdated", latestLocalTimestamp);

                QuerySnapshot firebaseSnapshot = await query.GetSnapshotAsync();

                if (firebaseSnapshot.Documents.Count() == 0)
                {
                    Debug.Log("Không có công việc mới để đồng bộ hóa. Quá trình hoàn tất.");
                    return true;
                }

                foreach (DocumentSnapshot doc in firebaseSnapshot.Documents)
                {
                    var taskData = doc.ToDictionary();

                    string firestoreDocId = doc.Id;

                    if (taskData.TryGetValue("content", out object contentValue) && taskData.TryGetValue("lastUpdated", out object lastUpdatedValue) && lastUpdatedValue is Timestamp firebaseTimestamp)
                    {
                        if (contentValue is string contentString)
                        {
                            DateTime taskTimestamp = firebaseTimestamp.ToDateTime();

                            var localTask = E_Task.FindEntity(entity => entity.f_Id == firestoreDocId);
                            if (localTask != null)
                            {
                                localTask.f_name = contentString;
                                localTask.f_location = taskData.TryGetValue("location", out object locationValue) ? locationValue.ToString() : "";
                                localTask.f_description = taskData.TryGetValue("description", out object descriptionValue) ? descriptionValue.ToString() : "";
                                localTask.f_createdBy = taskData.TryGetValue("createdBy", out object createdByValue) ? createdByValue.ToString() : "";
                                localTask.f_status = taskData.TryGetValue("status", out object statusValue) ? statusValue.ToString() : "";

                                // THÊM DÒNG NÀY ĐỂ KIỂM TRA NULL VÀ KHỞI TẠO DANH SÁCH
                                if (localTask.f_risks == null)
                                {
                                    localTask.f_risks = new List<int>();
                                }

                                localTask.f_risks.Clear();
                                if (taskData.TryGetValue("risks", out object risksObject) && risksObject is List<object> risksList)
                                {
                                    foreach (object riskItem in risksList)
                                    {
                                        if (riskItem is long riskIdLong)
                                        {
                                            localTask.f_risks.Add((int)riskIdLong);
                                        }
                                    }
                                }

                                localTask.f_lastUpdated = taskTimestamp;
                                Debug.Log($"Đã cập nhật công việc '{contentString}'.");
                            }
                            else
                            {
                                E_Task newEntity = E_Task.NewEntity();
                                newEntity.f_Id = firestoreDocId;
                                newEntity.f_name = contentString;
                                newEntity.f_location = taskData.TryGetValue("location", out object locationValue) ? locationValue.ToString() : "";
                                newEntity.f_description = taskData.TryGetValue("description", out object descriptionValue) ? descriptionValue.ToString() : "";
                                newEntity.f_createdBy = taskData.TryGetValue("createdBy", out object createdByValue) ? createdByValue.ToString() : "";
                                newEntity.f_status = taskData.TryGetValue("status", out object statusValue) ? statusValue.ToString() : "";

                                // Khởi tạo f_risks trước khi thêm
                                newEntity.f_risks = new List<int>();
                                if (taskData.TryGetValue("risks", out object risksObject) && risksObject is List<object> risksList)
                                {
                                    foreach (object riskItem in risksList)
                                    {
                                        if (riskItem is long riskIdLong)
                                        {
                                            newEntity.f_risks.Add((int)riskIdLong);
                                        }
                                    }
                                }

                                newEntity.f_lastUpdated = taskTimestamp;
                                Debug.Log($"Đã thêm công việc mới '{contentString}'.");
                            }
                        }
                    }
                }

                SaveData.Save();

                Debug.Log("Quá trình đồng bộ hóa công việc đã hoàn tất.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lỗi khi đồng bộ hóa dữ liệu công việc: {ex.Message}");
                return false;
            }
        }
    }
}
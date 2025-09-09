using UnityEngine;
using Firebase.Firestore;
using BansheeGz.BGDatabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace MySpace
{
    public class FirebaseToBGDatabasePurchaseSynchronizer : MonoBehaviour
    {
        public async Task<bool> SynchronizePurchasesFromFirebase(DateTime lastSyncTimestamp)
        {
            Debug.Log("Đang đồng bộ hóa lịch sử mua hàng từ Firebase xuống BGDatabase...");
            if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
            {
                Debug.LogError("Firebase không sẵn sàng. Không thể đồng bộ hóa lịch sử mua hàng.");
                return false;
            }

            try
            {
                CollectionReference purchasesCollection = FirebasePathUtils.GetPurchasesCollection(FirebaseManager.Instance.GetCanvasAppId(), FirebaseManager.Instance.db);

                Query newPurchasesQuery = purchasesCollection.WhereGreaterThan("timestamp", Timestamp.FromDateTime(lastSyncTimestamp.ToUniversalTime()));
                QuerySnapshot newPurchasesSnapshot = await newPurchasesQuery.GetSnapshotAsync();

                foreach (DocumentSnapshot purchaseDoc in newPurchasesSnapshot.Documents)
                {
                    var purchaseData = purchaseDoc.ToDictionary();
                    string firestoreDocId = purchaseDoc.Id;

                    string materialId = purchaseData.TryGetValue("materialId", out object idVal) ? idVal.ToString() : null;
                    int quantity = purchaseData.TryGetValue("quantity", out object quantityVal) ? (int)(long)quantityVal : 0;
                    string supplier = purchaseData.TryGetValue("supplier", out object supplierVal) ? supplierVal.ToString() : "";
                    string poNumber = purchaseData.TryGetValue("poNumber", out object poVal) ? poVal.ToString() : "";
                    DateTime timestamp = purchaseData.TryGetValue("timestamp", out object tsVal) ? ((Timestamp)tsVal).ToDateTime() : DateTime.MinValue;

                    if (materialId != null)
                    {
                        var localPurchase = E_PurchaseHistory.FindEntity(e => e.f_firestoreDocId == firestoreDocId);
                        if (localPurchase != null)
                        {
                            localPurchase.f_materialId = materialId;
                            localPurchase.f_quantity = quantity;
                            localPurchase.f_supplier = supplier;
                            localPurchase.f_name = poNumber;
                            localPurchase.f_timestamp = timestamp;
                            Debug.Log($"Đã cập nhật bản ghi mua hàng cục bộ cho vật tư {materialId}.");
                        }
                        else
                        {
                            var newPurchaseEntity = E_PurchaseHistory.NewEntity();
                            newPurchaseEntity.f_firestoreDocId = firestoreDocId;
                            newPurchaseEntity.f_materialId = materialId;
                            newPurchaseEntity.f_quantity = quantity;
                            newPurchaseEntity.f_supplier = supplier;
                            newPurchaseEntity.f_name = poNumber;
                            newPurchaseEntity.f_timestamp = timestamp;
                            Debug.Log($"Đã thêm mới bản ghi mua hàng cho vật tư {materialId}.");
                        }
                    }
                }
                
                SaveData.Save();
                Debug.Log("Quá trình đồng bộ hóa lịch sử mua hàng đã hoàn tất.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lỗi khi đồng bộ hóa lịch sử mua hàng: {ex.Message}");
                return false;
            }
        }
    }
}
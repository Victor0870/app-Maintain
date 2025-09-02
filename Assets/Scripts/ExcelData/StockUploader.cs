using UnityEngine;
using Firebase.Firestore;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

public class StockUploader : MonoBehaviour
{
    // Kéo thả file CSV vào đây từ cửa sổ Project hoặc nhập đường dẫn.
    public string csvFilePath = "Assets/SparePart.csv";

    public async void UploadStockData()
    {
        // 1. Kiểm tra kết nối Firebase
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
        {
            Debug.LogError("Firebase không sẵn sàng. Vui lòng kiểm tra FirebaseManager.");
            return;
        }

        // 2. Kiểm tra file
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError("Không tìm thấy file tại đường dẫn: " + csvFilePath);
            return;
        }

        string[] lines = File.ReadAllLines(csvFilePath);
        if (lines.Length <= 1)
        {
            Debug.LogWarning("File không có dữ liệu hoặc chỉ có tiêu đề.");
            return;
        }

        // Lấy tham chiếu đến collection "materials"
        CollectionReference materialsCollection = FirebaseManager.Instance.db
            .Collection("artifacts")
            .Document(FirebaseManager.Instance.GetCanvasAppId())
            .Collection("public")
            .Document("data")
            .Collection("materials");

        // Bắt đầu quá trình tải lên
        Debug.Log("Đang bắt đầu tải dữ liệu kho lên Firestore...");

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            // Đảm bảo có đủ 8 cột để tránh lỗi IndexOutOfRangeException
            if (values.Length < 8)
            {
                Debug.LogWarning($"Bỏ qua hàng {i} do thiếu dữ liệu.");
                continue;
            }

            string itemNo = values[0].Trim();

            // 3. Kiểm tra xem dữ liệu đã tồn tại trên Firestore hay chưa dựa trên cột "No"
            QuerySnapshot existingDocs = await materialsCollection.WhereEqualTo("No", itemNo).Limit(1).GetSnapshotAsync();
            if (existingDocs.Count > 0)
            {
                Debug.LogWarning($"Mục với 'No' = {itemNo} đã tồn tại trên Firestore. Bỏ qua.");
                continue;
            }

            int stockValue = 0;
            // Sử dụng int.TryParse để xử lý các giá trị không hợp lệ
            if (!int.TryParse(values[5].Trim(), out stockValue))
            {
                Debug.LogWarning($"Giá trị '{values[5].Trim()}' ở hàng {i} không phải là số. Mặc định đặt là 0.");
            }

            // Tạo một Dictionary để lưu trữ dữ liệu
            Dictionary<string, object> stockData = new Dictionary<string, object>
            {
                { "No", itemNo },
                { "name", values[1].Trim() },
                { "purpose", values[2].Trim() },
                { "type", values[3].Trim() }, // Mã sản phẩm
                { "unit", values[4].Trim() },
                { "stock", stockValue }, // Sử dụng giá trị đã được xử lý
                { "location", values[6].Trim() },
                { "category", values[7].Trim() },
                { "lastUpdated", FieldValue.ServerTimestamp }
            };

            // 4. Đẩy dữ liệu lên Firestore
            try
            {
                // Thêm document mới. Firebase sẽ tự tạo ID.
                await materialsCollection.AddAsync(stockData);
                Debug.Log($"Đã tải lên mục: {stockData["name"]}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Lỗi khi tải lên mục {stockData["name"]}: {ex.Message}");
            }
        }

        Debug.Log("Quá trình tải lên đã hoàn tất.");
    }
}
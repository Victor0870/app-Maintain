using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase.Extensions; // <<< THÊM DÒNG NÀY VÀO ĐÂY >>>

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseFirestore db;
    public FirebaseUser user; // Currently signed-in user
    public string userId; // ID of the currently signed-in user

    // This will store the appId from the environment.
    // In a real Canvas environment, __app_id is globally available.
    // For local Unity development, you might hardcode it or load from a config file.
    // IMPORTANT: For local testing, you MUST set this to a fixed string (e.g., "my-task-app-id").
    // When deploying to Canvas, ensure you use the actual __app_id provided by the environment.
    private string canvasAppId = "your-unity-app-id-placeholder"; // <<< CHÚ Ý: ĐỔI CÁI NÀY THÀNH ID ỨNG DỤNG CỦA BẠN HOẶC MỘT CHỖ ĐẶT TRƯỚC CHO VIỆC TEST CỤC BỘ >>>

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ đối tượng này giữa các cảnh
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CheckAndFixDependencies();
    }

    private void CheckAndFixDependencies()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Không thể giải quyết tất cả các phụ thuộc của Firebase: " + dependencyStatus);
            }
        });
    }

    private void InitializeFirebase()
    {
        Debug.Log("Đang khởi tạo Firebase...");
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null); // Gọi một lần để kiểm tra trạng thái hiện tại

        // Thử đăng nhập ẩn danh
        SignInAnonymously();
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = auth.CurrentUser != null;
            if (signedIn)
            {
                user = auth.CurrentUser;
                userId = user.UserId;
                Debug.LogFormat("Người dùng đã đăng nhập: {0} ({1})", user.DisplayName, userId);
                // Sau khi người dùng đăng nhập, bạn có thể bắt đầu lắng nghe các công việc từ Firestore
                // (Điều này sẽ được xử lý trong script TaskManager)
            }
            else
            {
                Debug.Log("Người dùng đã đăng xuất.");
                user = null;
                userId = null;
            }
        }
    }

    public async void SignInAnonymously()
    {
        try
        {
            // Đăng nhập ẩn danh
            await auth.SignInAnonymouslyAsync();
            Debug.Log("Đăng nhập ẩn danh thành công!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Lỗi khi đăng nhập ẩn danh: " + ex.Message);
        }
    }

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }

    // Getter cho canvasAppId
    public string GetCanvasAppId()
    {
        return canvasAppId;
    }
}

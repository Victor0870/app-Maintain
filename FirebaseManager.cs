using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase.Extensions;
using System.Linq;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseFirestore db;
    public FirebaseUser user; // Currently signed-in user
    public string userId; // ID of the currently signed-in user

    private string canvasAppId = "your-unity-app-id-placeholder";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        AuthStateChanged(this, null);

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
                // Kiểm tra và khởi tạo biến đếm tổng số công việc
                StartCoroutine(InitializeTaskCount());
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
            await auth.SignInAnonymouslyAsync();
            Debug.Log("Đăng nhập ẩn danh thành công!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Lỗi khi đăng nhập ẩn danh: " + ex.Message);
        }
    }

    private System.Collections.IEnumerator InitializeTaskCount()
    {
        DocumentReference totalCountRef = db.Collection("artifacts")
            .Document(canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("metadata")
            .Document(TaskConstants.TOTAL_TASK_COUNT_ID);

        var getTask = totalCountRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("Biến đếm tổng số công việc đã tồn tại.");
            }
            else
            {
                Debug.Log("Biến đếm tổng số công việc chưa tồn tại, đang khởi tạo...");
                CollectionReference tasksCollectionRef = db.Collection("artifacts")
                    .Document(canvasAppId)
                    .Collection("public")
                    .Document("data")
                    .Collection("tasks");
                
                tasksCollectionRef.Count().GetSnapshotAsync().ContinueWithOnMainThread(countTask => {
                    if (countTask.IsCompleted)
                    {
                        long count = countTask.Result.Count;
                        totalCountRef.SetAsync(new Dictionary<string, object> { { "count", count } });
                        Debug.Log($"Đã đếm và khởi tạo tổng số công việc là: {count}");
                    }
                });
            }
        });

        yield return new WaitUntil(() => getTask.IsCompleted);
    }

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }

    public string GetCanvasAppId()
    {
        return canvasAppId;
    }
}

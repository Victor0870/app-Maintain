using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseFirestore db;
    public FirebaseUser user;
    public string userId;

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
                InitializeTaskCount();
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

    private async Task InitializeTaskCount()
    {
        DocumentReference totalCountRef = db.Collection("artifacts")
            .Document(canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("metadata")
            .Document(TaskConstants.TOTAL_TASK_COUNT_ID);

        DocumentSnapshot snapshot = await totalCountRef.GetSnapshotAsync();

        if (snapshot.Exists)
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
            
            AggregateQuerySnapshot countSnapshot = await tasksCollectionRef.Count().GetSnapshotAsync();
            long count = countSnapshot.Count;
            await totalCountRef.SetAsync(new System.Collections.Generic.Dictionary<string, object> { { "count", count } });
            Debug.Log($"Đã đếm và khởi tạo tổng số công việc là: {count}");
        }
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

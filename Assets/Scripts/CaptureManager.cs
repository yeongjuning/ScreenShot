using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class CaptureManager : MonoBehaviour
{
    #region Fields * Properties
    [SerializeField] Button m_btnCapture;
    [SerializeField] Button m_btnCaptureWithoutUI;
    [SerializeField] Button m_btnReadFile;
    [SerializeField] CaptureFlash m_flash;
    [SerializeField] Image m_imgToShow;

    bool m_test = false;
    bool m_canCapture = false;
    Texture2D m_texture;

    const string FOLDER_NAME = "ScreenShots";
    const string FILE_NAME = "Cure_Baby";
    const string EXTENSION_NAME = "png";

    string RootPath
    {
        get
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Application.persistentDataPath;
#elif UNITY_ANDROID
            //return $"/storage/emulated/0/DCIM/{Application.productName}/";
            return Application.persistentDataPath;
#endif
        }
    }

    string FolderPath => $"{RootPath}/{FOLDER_NAME}";
    string TotalPath => $"{FolderPath}/{FILE_NAME}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.{EXTENSION_NAME}";
    string lastSavedPath;
    #endregion

    void Awake()
    {
        Debug.Log(FolderPath);

        m_imgToShow.gameObject.SetActive(false);
        m_btnCapture.onClick.AddListener(TakeCaptureFull);
        m_btnCaptureWithoutUI.onClick.AddListener(TakeCaptureWithoutUI);
        m_btnReadFile.onClick.AddListener(ReadScreenShotAndShow);
    }

    /// <summary>
    /// UI 제외하고 현재 카메라가 렌더링하는 모습 캡처 (Unity 제공 함수)
    /// </summary>
    void OnPostRender()
    {
        if (m_canCapture)
        {
            m_canCapture = false;
            CaptureScreenAndSave();
            ReadScreenShotAndShow();
        }
    }

    #region Callback Method
    /// <summary>
    /// UI 전체 화면 캡처
    /// </summary>
    void TakeCaptureFull()
    {
#if UNITY_ANDROID
        DoAfterCheckPermission(Permission.ExternalStorageWrite, () => StartCoroutine(CoTakeCaptureScreen()));
#else
        StartCoroutine(CoTakeCaptureScreen());
#endif
    }

    /// <summary>
    /// UI 미포함, 현재 카메라가 렌더링하는 화면만 캡처
    /// </summary>
    void TakeCaptureWithoutUI()
    {
#if UNITY_ANDROID
        DoAfterCheckPermission(Permission.ExternalStorageWrite, () => m_canCapture = true);
#else
        m_canCapture = true;
#endif
    }

    /// <summary>
    /// 경로로 부터 이미지 읽어오기
    /// </summary>
    void ReadScreenShotAndShow()
    {
#if UNITY_ANDROID
        DoAfterCheckPermission(Permission.ExternalStorageRead, () => ReadFile(m_imgToShow));
#else
        ReadFile(m_imgToShow);
#endif
    }
    #endregion

    IEnumerator CoTakeCaptureScreen()
    {
        yield return new WaitForEndOfFrame();

        CaptureScreenAndSave();
        ReadScreenShotAndShow(); // 바로 보여주기
    }

    /// <summary>
    /// 안드로이드 권한 확인 및 승인, 승인 후 동작 수행
    /// </summary>
    void DoAfterCheckPermission(string permission, Action actionPermissionGranted)
    {
#if UNITY_ANDROID
        // 안드로이드 저장소 권한 확인하고 요청
        if (Permission.HasUserAuthorizedPermission(permission) == false)
        {
            var callbacks = new PermissionCallbacks();
#if UNITY_EDITOR
            callbacks.PermissionGranted += str => Debug.Log($"{str} 승인");
#endif
            callbacks.PermissionGranted += str => AndToastMessage.Instance.ShowToastMessage($"{str} 권한을 승인하셨습니다.");
            callbacks.PermissionGranted += _ => actionPermissionGranted();

#if UNITY_EDITOR
            callbacks.PermissionDenied += str => Debug.Log($"{str} 거절");
#endif
            callbacks.PermissionDenied += str => AndToastMessage.Instance.ShowToastMessage($"{str} 권한을 거절하셨습니다.");

            Permission.RequestUserPermission(permission, callbacks);
        }
        else
        {
            actionPermissionGranted.Invoke();   // 바로 실행
        }
#endif
    }

    /// <summary>
    /// 캡처하고 찍고 경로에 저장하기
    /// </summary>
    void CaptureScreenAndSave()
    {
        string totalPath = TotalPath;
        var screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        var area = new Rect(0f, 0f, Screen.width, Screen.height);
        bool succeeded = true;

        //현재 스크린으로부터 지정 영역의 픽셀들을 텍스쳐에 저장
        screenTexture.ReadPixels(area, 0, 0);

        try
        {
            if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
            File.WriteAllBytes(totalPath, screenTexture.EncodeToPNG());
        }
        catch (Exception e)
        {
            succeeded = false;
            Debug.LogException(e);
            throw;
        }

        Destroy(screenTexture);

        if (succeeded)
        {
#if UNITY_EDITOR
            Debug.Log($"Screen Shot Saved : {totalPath}");
#endif
            m_flash?.Show();                         // 화면 플래쉬
            lastSavedPath = totalPath;               // 최근 경로에 저장
            m_imgToShow.gameObject.SetActive(true);  // 스크린샷 헀던 보여줄 이미지 활성화
            RefreshAndroidGallery(totalPath);        // 갤러리 갱신            
        }
    }

    /// <summary>
    /// 갤러리 갱신
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_ANDROID")]
    void RefreshAndroidGallery(string imageFilePath)
    {
#if !UNITY_EDITOR
            AndroidJavaClass classPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject objActivity = classPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass classUri = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject objIntent = new AndroidJavaObject("android.content.Intent", new object[2]
            { "android.intent.action.MEDIA_SCANNER_SCAN_FILE", classUri.CallStatic<AndroidJavaObject>("parse", "file://" + imageFilePath) });
            objActivity.Call("sendBroadcast", objIntent);
#endif
        StartCoroutine(CoFadeoutShowPanel());
    }

    // 가장 최근에 경로로부터 저장된 스크린샷 파일을 읽어서 이미지에 보여주기
    void ReadFile(Image destination)
    {
        string folderPath = FolderPath;
        string totalPath = lastSavedPath;

        if (Directory.Exists(folderPath) == false)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{folderPath} 폴더가 존재하지 않습니다.");
#endif
            return;
        }

        if (File.Exists(totalPath) == false)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{totalPath} 파일이 존재하지 않습니다.");
#endif
            return;
        }

        // 기존의 텍스쳐 소스 제거
        if (m_texture != null) Destroy(m_texture);
        if (destination.sprite != null)
        {
            Destroy(destination.sprite);
            destination.sprite = null;
        }

        ReadFileForPath(totalPath);
        ApplySpriteImage(totalPath, destination);
    }

    /// <summary>
    /// 저장된 스크린샷 파일 경로로부터 읽어오기
    /// </summary>
    void ReadFileForPath(string totalPath)
    {
        // 저장된 스크린샷 파일 경로로부터 읽어오기
        try
        {
            var texBuffer = File.ReadAllBytes(totalPath);
            m_texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
            m_texture.LoadImage(texBuffer);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogException(e);
#endif
            throw;
        }
    }

    void ApplySpriteImage(string totalPath, Image dest)
    {
        var rect = new Rect(0, 0, m_texture.width, m_texture.height);
        var sprite = Sprite.Create(m_texture, rect, Vector2.one * 0.5f);
        dest.sprite = sprite;
    }

    #region Fade Out
    float timer = 2.5f;

    IEnumerator CoFadeoutShowPanel()
    {        
        // 2초뒤에 점점 사라지도록 설정
        yield return new WaitForSeconds(2f);

        Color color = m_imgToShow.color;
        while (color.a >= 0f)
        {
            color.a -= Time.deltaTime / timer;
            m_imgToShow.color = color;
            yield return null;
        }

        m_imgToShow.gameObject.SetActive(false);
        Color refreshColor = m_imgToShow.color;
        refreshColor.a = 1f;
        m_imgToShow.color = refreshColor;
    }
    #endregion
}

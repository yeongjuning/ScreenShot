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
    /// UI �����ϰ� ���� ī�޶� �������ϴ� ��� ĸó (Unity ���� �Լ�)
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
    /// UI ��ü ȭ�� ĸó
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
    /// UI ������, ���� ī�޶� �������ϴ� ȭ�鸸 ĸó
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
    /// ��η� ���� �̹��� �о����
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
        ReadScreenShotAndShow(); // �ٷ� �����ֱ�
    }

    /// <summary>
    /// �ȵ���̵� ���� Ȯ�� �� ����, ���� �� ���� ����
    /// </summary>
    void DoAfterCheckPermission(string permission, Action actionPermissionGranted)
    {
#if UNITY_ANDROID
        // �ȵ���̵� ����� ���� Ȯ���ϰ� ��û
        if (Permission.HasUserAuthorizedPermission(permission) == false)
        {
            var callbacks = new PermissionCallbacks();
#if UNITY_EDITOR
            callbacks.PermissionGranted += str => Debug.Log($"{str} ����");
#endif
            callbacks.PermissionGranted += str => AndToastMessage.Instance.ShowToastMessage($"{str} ������ �����ϼ̽��ϴ�.");
            callbacks.PermissionGranted += _ => actionPermissionGranted();

#if UNITY_EDITOR
            callbacks.PermissionDenied += str => Debug.Log($"{str} ����");
#endif
            callbacks.PermissionDenied += str => AndToastMessage.Instance.ShowToastMessage($"{str} ������ �����ϼ̽��ϴ�.");

            Permission.RequestUserPermission(permission, callbacks);
        }
        else
        {
            actionPermissionGranted.Invoke();   // �ٷ� ����
        }
#endif
    }

    /// <summary>
    /// ĸó�ϰ� ��� ��ο� �����ϱ�
    /// </summary>
    void CaptureScreenAndSave()
    {
        string totalPath = TotalPath;
        var screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        var area = new Rect(0f, 0f, Screen.width, Screen.height);
        bool succeeded = true;

        //���� ��ũ�����κ��� ���� ������ �ȼ����� �ؽ��Ŀ� ����
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
            m_flash?.Show();                         // ȭ�� �÷���
            lastSavedPath = totalPath;               // �ֱ� ��ο� ����
            m_imgToShow.gameObject.SetActive(true);  // ��ũ���� ���� ������ �̹��� Ȱ��ȭ
            RefreshAndroidGallery(totalPath);        // ������ ����            
        }
    }

    /// <summary>
    /// ������ ����
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

    // ���� �ֱٿ� ��ηκ��� ����� ��ũ���� ������ �о �̹����� �����ֱ�
    void ReadFile(Image destination)
    {
        string folderPath = FolderPath;
        string totalPath = lastSavedPath;

        if (Directory.Exists(folderPath) == false)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{folderPath} ������ �������� �ʽ��ϴ�.");
#endif
            return;
        }

        if (File.Exists(totalPath) == false)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{totalPath} ������ �������� �ʽ��ϴ�.");
#endif
            return;
        }

        // ������ �ؽ��� �ҽ� ����
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
    /// ����� ��ũ���� ���� ��ηκ��� �о����
    /// </summary>
    void ReadFileForPath(string totalPath)
    {
        // ����� ��ũ���� ���� ��ηκ��� �о����
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
        // 2�ʵڿ� ���� ��������� ����
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

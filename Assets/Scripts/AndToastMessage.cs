using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndToastMessage : MonoBehaviour
{
    public enum ToastLengthType
    {
        Short,      // �� 2.5
        Long        // �� 4��
    }

    #region SingleTon
    public static AndToastMessage Instance
    {
        get 
        {
            if (instance == null)
            {
                GameObject contaniner = new GameObject("AndroidToast Singleton Container");
                instance = contaniner.AddComponent<AndToastMessage>();
            }
            return instance;
        }
    }
    #endregion

    static AndToastMessage instance;

    void Awake()
    {
        CheckInstance();
    }

#if UNITY_EDITOR
    float m_editorGUITime = 0f;
    string m_editorGUIMessage;
#elif UNITY_ANDROID
    AndroidJavaClass unityPlayer;
    AndroidJavaObject unityActivity;
    AndroidJavaClass toastClass;

    private void Start()
    {
        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        toastClass = new AndroidJavaClass("android.widget.Toast");
    }
#endif

    void CheckInstance()
    {
        if (instance == null) instance = this;

        // �̱��� �����ϴµ�, ������ �ƴ� ���, ������Ʈ �ı�
        if (instance != null && instance != this)
        {
            Destroy(this);
            var components = gameObject.GetComponents<Component>();
            if (components.Length <= 2) Destroy(gameObject);

            UnityEngine.Debug.Log("�̹� AndroidToast �̱����� �����ϹǷ� ������Ʈ�� �ı��մϴ�.");
        }
    }

    /// <summary>
    /// �ȵ���̵� �佺Ʈ �޽��� ǥ���ϱ�
    /// </summary>
    [Conditional("UNITY_ANDROID")]
    public void ShowToastMessage(string msg, ToastLengthType length = ToastLengthType.Short)
    {
#if UNITY_EDITOR
        m_editorGUITime = length == ToastLengthType.Short ? 2.5f : 4f;
        m_editorGUIMessage = msg;
#elif UNITY_ANDROID
        if (unityActivity != null)
        {
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, msg, (int)length);
                toastObject.Call("show");
            }));
        }
#endif
    }

#if UNITY_EDITOR
    GUIStyle toastStyle;
    void OnGUI()
    {
        if (m_editorGUITime <= 0f) return;

        float width = Screen.width * 0.5f;
        float height = Screen.height * 0.08f;
        var rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.8f, width, height);

        if (toastStyle == null)
        {
            toastStyle = new GUIStyle(GUI.skin.box);
            toastStyle.fontSize = 36;
            toastStyle.fontStyle = FontStyle.Bold;
            toastStyle.alignment = TextAnchor.MiddleCenter;
            toastStyle.normal.textColor = Color.black;
        }

        GUI.Box(rect, m_editorGUIMessage, toastStyle);
    }

    void Update()
    {
        if (m_editorGUITime > 0f)
            m_editorGUITime -= Time.unscaledDeltaTime;
    }
#endif
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndInputManager : MonoBehaviour
{
#if UNITY_ANDROID
    bool m_preparedToQuit = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_preparedToQuit == false)
            {
                AndToastMessage.Instance.ShowToastMessage("�ڷΰ��� ��ư�� �� �� �� �����ø� ����˴ϴ�.");
                StartCoroutine(CoPrepareToQuit());
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }

    IEnumerator CoPrepareToQuit()
    {
        m_preparedToQuit = true;
        yield return new WaitForSecondsRealtime(2.5f);
        m_preparedToQuit = false;
    }
#endif
}

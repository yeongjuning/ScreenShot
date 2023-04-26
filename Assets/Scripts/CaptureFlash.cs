using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CaptureFlash : MonoBehaviour
{
    [SerializeField] float m_duration = 0.3f;

    Image m_img;
    float m_currentAlpha = 1f;

    void Awake()
    {
        m_img = GetComponent<Image>();
    }

    void Update()
    {
        var color = m_img.color;
        color.a = m_currentAlpha;
        m_img.color = color;

        m_currentAlpha -= Time.unscaledDeltaTime / m_duration;

        if (m_currentAlpha < 0f) gameObject.SetActive(false);
    }

    public void Show()
    {
        m_currentAlpha = 1f;
        gameObject.SetActive(true);
    }
}

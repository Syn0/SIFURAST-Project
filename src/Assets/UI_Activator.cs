using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UI_Activator : MonoBehaviour {

    Button _btn;
    Text txt_btn;
    public RectTransform rect;
    public string hide_text = "X";
    public string show_text = "V";
    public float anim_speed = 20f;
    public bool default_isShowed = true;
    bool showValue;
    bool isAnimating = false;
    private void Awake()
    {
        if (rect==null) throw new Exception("Composant manquant !");
        _btn = GetComponent<Button>();
        txt_btn = GetComponentInChildren<Text>();

        showValue = default_isShowed;
        rect.gameObject.SetActive(showValue);

        if (default_isShowed) txt_btn.text = show_text;
        else txt_btn.text = hide_text;

        _btn.onClick.AddListener(switchDisplay);
    }

    public void switchDisplay()
    {
        if (isAnimating) return;
        if (showValue) hide();
        else show();
    }

    public void show()
    {
        showValue = true;
        StartCoroutine(co_Show());
    }

    IEnumerator co_Show()
    {
        Vector3 s = rect.localScale;
        s.y = 0f;
        rect.localScale = s;
        rect.gameObject.SetActive(true);
        isAnimating = true;

        while (s.y<0.98f)
        {
            s.y = Mathf.Lerp(s.y, 1f, Time.deltaTime * anim_speed);
            rect.localScale = s;
            yield return null;
        }

        s.y = 1f;
        rect.localScale = s;
        rect.gameObject.SetActive(true);
        isAnimating = false;
    }

    public void hide()
    {
        showValue = false;
        StartCoroutine(co_Hide());
    }

    IEnumerator co_Hide()
    {
        Vector3 s = rect.localScale;
        s.y = 1f;
        rect.localScale = s;
        rect.gameObject.SetActive(true);
        isAnimating = true;

        while (s.y > 0.02f)
        {
            s.y = Mathf.Lerp(s.y, 0f, Time.deltaTime * anim_speed);
            rect.localScale = s;
            yield return null;
        }

        s.y = 1f;
        rect.localScale = s;
        rect.gameObject.SetActive(false);
        isAnimating = false;
    }

}

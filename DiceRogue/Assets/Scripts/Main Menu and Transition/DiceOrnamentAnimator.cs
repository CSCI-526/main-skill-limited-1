using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiceOrnamentAnimator : MonoBehaviour
{
    [Tooltip("Assign 9 pip Images in reading order (1..9): 1 2 3 / 4 5 6 / 7 8 9")]
    public List<Image> pips = new List<Image>(9);

    [Header("Idle animation")]
    public bool playOnEnable = true;
    public float changeIntervalMin = 0.6f;
    public float changeIntervalMax = 1.2f;

    [Header("Faces 1..6 to show")]
    public int currentFace = 5;

    private Coroutine loop;
    private bool isPaused;

    private void OnEnable()
    {
        AutoFindPipsIfEmpty();
        SetFace(currentFace);
        if (playOnEnable) Play();
    }

    public void Play()
    {
        if (loop != null) StopCoroutine(loop);
        isPaused = false;
        loop = StartCoroutine(IdleLoop());
    }

    public void Pause()
    {
        isPaused = true;
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    private IEnumerator IdleLoop()
    {
        while (!isPaused)
        {
            int next = Random.Range(1, 7); // 1..6
            SetFace(next);
            yield return new WaitForSeconds(Random.Range(changeIntervalMin, changeIntervalMax));
        }
    }

    public void SetFace(int face)
    {
        currentFace = Mathf.Clamp(face, 1, 6);
        if (pips == null || pips.Count < 9) return;

        // turn all off
        for (int i = 0; i < 9; i++)
            if (pips[i] != null) pips[i].enabled = false;

        // enable indices for chosen face
        switch (currentFace)
        {
            case 1: Enable(5); break;
            case 2: Enable(1, 9); break;
            case 3: Enable(1, 5, 9); break;
            case 4: Enable(1, 3, 7, 9); break;
            case 5: Enable(1, 3, 5, 7, 9); break;
            case 6: Enable(1, 3, 4, 6, 7, 9); break;
        }
    }

    private void Enable(params int[] idx)
    {
        foreach (var k in idx)
        {
            int i = k - 1;
            if (i >= 0 && i < pips.Count && pips[i] != null)
                pips[i].enabled = true;
        }
    }

    private void AutoFindPipsIfEmpty()
    {
        if (pips != null && pips.Count >= 9 && pips[0] != null) return;

        var found = new List<Image>();
        for (int i = 1; i <= 9; i++)
        {
            var t = transform.Find("Pip_" + i);
            if (t != null && t.TryGetComponent<Image>(out var img))
                found.Add(img);
        }
        if (found.Count == 9) { pips = found; return; }

        found.Clear();
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Image>(out var img))
            {
                found.Add(img);
                if (found.Count == 9) break;
            }
        }
        if (found.Count == 9) pips = found;
    }
}

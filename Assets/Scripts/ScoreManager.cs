using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Local score + optional UI for a single air-hockey table.
/// There are no static fields: every Environment prefab owns its own instance.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /* ────────── Settings ────────── */
    [Header("Show UI?  (untick while training)")]
    [SerializeField] bool uiEnabled = true;

    [Header("Score text references (TMP)")]
    [SerializeField] TextMeshProUGUI p1Text;      // blue, Player1
    [SerializeField] TextMeshProUGUI p2Text;      // orange, Player2

    [Header("Popup message")]
    [SerializeField] TextMeshProUGUI goalPopup;
    [Tooltip("Seconds the popup stays visible (0 = disabled).")]
    [SerializeField] float popupSeconds = 3f;

    [Header("Win condition")]
    [SerializeField] int pointsToWin = 7;

    /* ────────── Runtime state ────────── */
    int p1 = 0;   // current score blue
    int p2 = 0;   // current score orange
    Coroutine popupRoutine;

    /* ────────── Initialisation ────────── */
    void Start()
    {
        if (!uiEnabled)
        {
            if (p1Text)    p1Text.gameObject.SetActive(false);
            if (p2Text)    p2Text.gameObject.SetActive(false);
            if (goalPopup) goalPopup.gameObject.SetActive(false);
            return;
        }

        UpdateTexts();
        if (goalPopup) goalPopup.gameObject.SetActive(false);
    }

    /* ────────── Public API (used by GoalDetector) ────────── */

    /// <summary>Adds a point and updates UI.  
    /// Returns <c>true</c> if someone reached <see cref="pointsToWin"/>.</summary>
    public bool AddScore(string who)
    {
        if (who == "Player1")      // Blue scores
        {
            p1++;
            ShowPopup("BLUE SCORED!",  new Color32( 54,173,255,255));
        }
        else if (who == "Player2") // Orange scores
        {
            p2++;
            ShowPopup("ORANGE SCORED!", new Color32(255,140, 30,255));
        }

        UpdateTexts();
        return (p1 >= pointsToWin || p2 >= pointsToWin);
    }

    /// <summary>Sets both scores to zero and refreshes UI.</summary>
    public void ResetScore()
    {
        p1 = p2 = 0;
        UpdateTexts();
    }

    /* ────────── Internal helpers ────────── */

    void UpdateTexts()
    {
        if (!uiEnabled) return;
        if (p1Text) p1Text.text = p1.ToString();
        if (p2Text) p2Text.text = p2.ToString();
    }

    void ShowPopup(string msg, Color col)
    {
        if (!uiEnabled || popupSeconds <= 0f || goalPopup == null) return;

        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupRoutine(msg, col));
    }

    IEnumerator PopupRoutine(string msg, Color col)
    {
        goalPopup.text  = msg;
        goalPopup.color = col;
        goalPopup.gameObject.SetActive(true);

        yield return new WaitForSeconds(popupSeconds);

        goalPopup.gameObject.SetActive(false);
        popupRoutine = null;
    }
}

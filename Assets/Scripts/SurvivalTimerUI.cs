using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SurvivalTimerUI : MonoBehaviour
{
    [Header("計時器")]
    [SerializeField]
    private SurvivalTimer survivalTimer;

    [Header("顯示文字")]
    [SerializeField]
    private Text timerText;

    private int previousSeconds = -1;

    private void Start()
    {
        if (survivalTimer == null || timerText == null)
        {
            Debug.LogError("生存時間 UI 初始化失敗，請確認參照。", this);
            enabled = false;
            return;
        }

        RefreshUI();
    }

    private void Update()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (survivalTimer == null || timerText == null)
        {
            return;
        }

        int totalSeconds =
            Mathf.FloorToInt(survivalTimer.ElapsedSeconds);

        if (totalSeconds == previousSeconds)
        {
            return;
        }

        timerText.text = FormatSeconds(survivalTimer.ElapsedSeconds);
        previousSeconds = totalSeconds;
    }

    public static string FormatSeconds(float elapsedSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultsScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI finalScoreText;

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            finalScoreText.text = ScoreManager.Instance.FinalScore.ToString("000");
        }
        else
        {
            finalScoreText.text = "???";
        }
    }
}

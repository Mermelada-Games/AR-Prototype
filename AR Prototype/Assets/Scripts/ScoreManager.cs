using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public int FinalScore { get; private set; }

    private int score = 100;
    private int hits = 0;

    public TextMeshProUGUI scoreText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateScoreText();
    }

    public void RegisterHit()
    {
        hits++;

        if (hits > 1)
        {
            score -= 10;
            if (score < 0) score = 0;
        }

        UpdateScoreText();
        Debug.Log("Hit registrado. Score: " + score);
        
    }



    public void BallEnteredHole()
    {
        FinalScore = score;
    }


    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString("000");
        }
        else
        {
            Debug.LogWarning("ScoreText UI no está asignado.");
        }
    }
}

using UnityEngine;

public class BallHitDetector : MonoBehaviour
{
    private bool wasHit = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Club"))
        {
            if (!wasHit)
            {
                ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
                if (scoreManager != null)
                {
                    scoreManager.RegisterHit();
                    
                }

                wasHit = true;
                Invoke(nameof(ResetHit), 0.5f);
            }
        }
    }

    void ResetHit()
    {
        wasHit = false;
    }
}

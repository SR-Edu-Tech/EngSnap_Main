using UnityEngine;
using System.Collections;

public class SpeakingAutoNext : MonoBehaviour
{
    public WordMatchEvaluator evaluator;
    public float checkDelay = 0.3f;

    bool completed = false;

    void OnEnable()
    {
        completed = false;
        StartCoroutine(CheckLoop());
    }

    IEnumerator CheckLoop()
    {
        while (!completed)
        {
            yield return new WaitForSeconds(checkDelay);

            //if (evaluator != null && evaluator.CurrentScore >= evaluator.passThreshold)
          //  {
               // completed = true;

                // Move to next slide in ListeningGameplay
                //ListeningGameplay.Instance.SendMessage("OnSlideComplete");
           // }
        }
    }
}
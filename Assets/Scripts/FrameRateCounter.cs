using UnityEngine;
using UnityEngine.UI;

public class FrameRateCounter : MonoBehaviour
{

    [SerializeField]
    Text display;

    public enum DisplayMode { FPS, MS }

    [SerializeField]
    DisplayMode displayMode = DisplayMode.FPS;

    [SerializeField, Range(0.1f, 2f)]
    float sampleDuration = 1f;

    int frames;
    float duration, bestDuration = float.MaxValue, worstDuration;

    private void Update()
    {
        float frameDuration = Time.unscaledDeltaTime;
        frames += 1;
        duration += frameDuration;

        if (frameDuration < bestDuration)
        {
            bestDuration = frameDuration;
        }

        if (frameDuration > worstDuration)
        {
            worstDuration = frameDuration;
        }

        // show average frame rate within duration seconds
        if (duration >= sampleDuration)
        {
            if (displayMode == DisplayMode.FPS)
            {
                display.text = "FPS\n" 
                    + Mathf.Round(1f / bestDuration) + "\n"
                    + Mathf.Round(frames / duration) + "\n"
                    + Mathf.Round(1f / worstDuration);
            }
            else
            {
                display.text = "MS\n"
                    + System.Math.Round(1000f * bestDuration, 1) + "\n"
                    + System.Math.Round(1000f * duration / frames, 1) + "\n"
                    + System.Math.Round(1000f * worstDuration, 1) * 1.0f;
            }

            frames = 0;
            duration = 0f;

            bestDuration = float.MaxValue;
            worstDuration = 0f;
        }
    }
}
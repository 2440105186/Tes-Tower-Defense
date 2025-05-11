using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    
    private void Update()
    {
        var currentFps = 1f / Time.unscaledDeltaTime;
        text.text = ($"FPS: {currentFps:F1}");
    }
}

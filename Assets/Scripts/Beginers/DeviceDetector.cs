using UnityEngine;

public class DeviceDetector : MonoBehaviour
{
    public enum DeviceCategory { Desktop, Tablet, Phone }

    public static DeviceCategory GetDevice()
    {
        if (SystemInfo.deviceType == DeviceType.Desktop)
            return DeviceCategory.Desktop;

        float dpi = Screen.dpi > 0 ? Screen.dpi : 160f; // fallback if dpi is 0
        float w = Screen.width  / dpi;
        float h = Screen.height / dpi;
        float diagonal = Mathf.Sqrt(w * w + h * h);

        return diagonal >= 6.5f ? DeviceCategory.Tablet : DeviceCategory.Phone;
    }

    void Start()
    {
        switch (GetDevice())
        {
            case DeviceCategory.Tablet:
                Debug.Log("Tablet detected");
                // load tablet layout
                break;
            case DeviceCategory.Phone:
                Debug.Log("Phone detected");
                // load phone layout
                break;
            case DeviceCategory.Desktop:
                Debug.Log("Desktop detected");
                break;
        }
    }
}
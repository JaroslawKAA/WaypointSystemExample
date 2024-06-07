using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class WaypointIndicatorController : MonoBehaviour
{
    // SERIALIZED
    [Title("Config")]
    [SerializeField] int screenEdgesMargin = 50;
    [SerializeField] float smoothStrength = 10f;
    [SerializeField] float rotationOffset = 90f;
    [SerializeField] float pulsingSpeed = 1f;
    [SerializeField] float pulsingDistance = 20f;

    [Title("Depend")]
    [SerializeField] [Required] RectTransform indicatorRect;
    [SerializeField] [Required] Image indicatorImage;
    [PropertySpace]
    [SerializeField] [Required] Sprite arrowSprite;
    [SerializeField] [Required] Sprite onScreenSprite;

    [Title("Debug")]
    [SerializeField] bool rotateArrow = true;
    [SerializeField] bool clampPosition = true;
    [SerializeField] bool smooth = true;
    [SerializeField] bool pulsingEnabled = true;

    // PRIVATE
    Transform cameraTransform;
    Vector2 targetPosition;
    
    bool isTargetOnScreen = false;
    bool isPulsingOn = false; // If target is on cam, arrow is jumping
    float pulsingYValue = 0f;
    
    readonly Vector2 canvasScalerResolution = new(1920, 1080);

    // PROPERTIES
    public static WaypointIndicatorController Instance { get; private set; }
    Transform Target { get; set; }
    Camera Camera { get; set; }

    // UNITY EVENTS
    void OnEnable()
    {
        Camera = Camera.main;
        cameraTransform = Camera.transform;

        indicatorImage.enabled = Target != null;

        Instance = this;
        // Let's update after cinemachine main camera updated, because cinamachine works in LateUpdate
        EventBus.onPlayerCameraMoved.AddListener(OnPlayerCameraMoved);
    }


    void OnDisable()
    {
        Instance = null;
        EventBus.onPlayerCameraMoved.RemoveListener(OnPlayerCameraMoved);
    }

    // METHODS
    [Button]
    public void SetTarget(Transform target)
    {
        SetIndicator(withSmooth: false);
        Target = target;
        indicatorImage.enabled = true;
    }

    [Button]
    public void RemoveTarget()
    {
        Target = null;
        indicatorImage.enabled = false;
    }

    void OnPlayerCameraMoved()
    {
        SetIndicator(withSmooth: true);
    }

    void SetIndicator(bool withSmooth)
    {
        if (!Target) return;

        // Check if Target is in front of player
        if (cameraTransform.IsInFront(Target))
        {
            indicatorImage.sprite = onScreenSprite;
            Vector2 screenPoint = GetScreenPosition();

            isTargetOnScreen = Camera.IsPointInCameraView(Target.position);
            if (isTargetOnScreen)
            {
                indicatorRect.rotation = Quaternion.identity;
                if (!isPulsingOn) isPulsingOn = true;
            }
            else
            {
                indicatorImage.sprite = arrowSprite;
                if (clampPosition) screenPoint = ClampIndicatorPosition(screenPoint);
                if (rotateArrow) RotateIndicator(screenPoint);
                if (isPulsingOn) isPulsingOn = false;
            }

            if (withSmooth) SetSmoothPosition(screenPoint);
            else SetRawPosition(screenPoint);
        }
        // Not in front of camera
        else
        {
            SetRightOrLeftConstantPosition();
        }
    }

    void SetSmoothPosition(Vector2 screenPoint)
    {
        Vector2 rawScreenPoint = targetPosition == Vector2.zero ? indicatorRect.anchoredPosition : targetPosition;
        Vector2 smoothScreenPoint = Vector3.Lerp(a: rawScreenPoint, b: screenPoint, t: smoothStrength * Time.deltaTime);

        targetPosition = smoothScreenPoint;

        if (isPulsingOn && pulsingEnabled)
        {
            pulsingYValue = GetPulsingValue();
            indicatorRect.anchoredPosition = targetPosition + new Vector2(0, pulsingYValue);
        }
        else
        {
            indicatorRect.anchoredPosition = targetPosition;
        }
    }

    float GetPulsingValue()
    {
        return Mathf.Cos(Time.time * pulsingSpeed) * pulsingDistance;
    }

    void SetRawPosition(Vector2 screenPoint)
    {
        targetPosition = screenPoint;

        if (isPulsingOn && pulsingEnabled)
        {
            pulsingYValue = GetPulsingValue();
            indicatorRect.anchoredPosition = targetPosition + new Vector2(0, pulsingYValue);
        }
        else
        {
            indicatorRect.anchoredPosition = targetPosition;
        }
    }

    void SetRightOrLeftConstantPosition()
    {
        indicatorImage.sprite = arrowSprite;

        Vector3 targetDir = Target.position - cameraTransform.position;
        Vector3 forward = cameraTransform.forward;
        float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);

        if (angle > 0)
        {
            SetIndicatorToLeft();
        }
        else
        {
            SetIndicatorToRight();
        }
    }

    Vector2 GetScreenPosition()
    {
        Vector2 screenPoint = Camera.WorldToScreenPoint(Target.position);
        Vector2 position01 = screenPoint / new Vector2(Camera.pixelWidth, Camera.pixelHeight);
        return new Vector2(canvasScalerResolution.x * position01.x, canvasScalerResolution.y * position01.y);
    }

    void RotateIndicator(Vector2 screenPoint)
    {
        Vector2 screenCenter = new(canvasScalerResolution.x / 2,
            canvasScalerResolution.y / 2);
        Vector2 arrowDirection = (screenPoint - screenCenter).normalized;
        float angle = Mathf.Atan2(arrowDirection.y, arrowDirection.x) * Mathf.Rad2Deg;
        indicatorRect.rotation = Quaternion.AngleAxis(angle + rotationOffset, Vector3.forward);
    }

    /// <summary>
    /// Crops a point on the screen to the margin space
    /// </summary>
    /// <param name="screenPoint">The point in the camera view</param>
    /// <returns>Clamped screen point</returns>
    Vector2 ClampIndicatorPosition(Vector2 screenPoint)
    {
        return new Vector2(x: Mathf.Clamp(value: screenPoint.x,
                min: screenEdgesMargin,
                max: canvasScalerResolution.x - screenEdgesMargin),
            y: Mathf.Clamp(value: screenPoint.y,
                min: screenEdgesMargin,
                max: canvasScalerResolution.y - screenEdgesMargin));
    }

    /// <summary>
    /// Indicates the target is out of camera view on the right.
    /// </summary>
    void SetIndicatorToRight()
    {
        Vector2 indicatorPosition =
            new(x: canvasScalerResolution.x - screenEdgesMargin, y: canvasScalerResolution.y / 2);

        indicatorRect.anchoredPosition = indicatorPosition;
        indicatorRect.rotation = Quaternion.Euler(0, 0, 90);
    }

    /// <summary>
    /// Indicates the target is out of camera view on the left.
    /// </summary>
    void SetIndicatorToLeft()
    {
        Vector2 indicatorPosition = new(x: screenEdgesMargin, y: canvasScalerResolution.y / 2);

        indicatorRect.anchoredPosition = indicatorPosition;
        indicatorRect.rotation = Quaternion.Euler(0, 0, -90);
    }
}
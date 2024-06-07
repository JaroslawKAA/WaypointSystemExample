using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Collider))]
public class Waypoint : MonoBehaviour
{
    const string PlayerTag = "Player";
    
    [Title("Depend")]
    [SerializeField] [Required] Transform frontPoint;
    [SerializeField] [Required] Transform backPoint;
    [SerializeField] [Required] Transform cachedTransform;

    [Title("Debug")]
    [SerializeField] [ReadOnly] bool playerWentThrow = false;
    [SerializeField] [ReadOnly] bool playerWentBack = false;
    
    public Transform FrontPoint => frontPoint;
    public Transform BackPoint => backPoint;
    public Transform Transform => cachedTransform;

    public event Action onPlayerWentThrowWaypoint;
    public event Action onPlayerReturnedWaypoint;

#if UNITY_EDITOR
    void Awake()
    {
        Assert.IsTrue(GetComponent<Collider>().isTrigger);
    }
#endif

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PlayerTag))
        {
            if (!TargetIsInFrontOf(other.transform))
            {
                playerWentThrow = true;
            }
            else
            {
                playerWentBack = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PlayerTag))
        {
            if (TargetIsInFrontOf(other.transform))
            {
                if (playerWentThrow)
                {
                    onPlayerWentThrowWaypoint?.Invoke();
                }
            }
            else
            {
                if (playerWentBack)
                {
                    onPlayerReturnedWaypoint?.Invoke();
                }
            }

            playerWentThrow = false;
            playerWentBack = false;
        }
    }

    bool TargetIsInFrontOf(Transform target)
    {
        Vector3 otherPosition = target.position;
        Vector3 vectorToTarget = otherPosition - Transform.position;
        float dot = Vector3.Dot(vectorToTarget, Transform.forward);
        if (dot > 0)
        {
            Debug.Log("Target is in front");
            return true;
        }
        else
        {
            Debug.Log("Target is not in front");
            return false;
        }
    }
}
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UltEvents.Helpers.Debug;

public class WaypointsPath : MonoBehaviour
{
    [Title("Config")]
    [SerializeField] List<Waypoint> waypoints = new();

    [Title("Debug")]
    [SerializeField] [ReadOnly]
    int currentWaypoint = 0;

    public event Action onPathFinished;
    
    void OnEnable()
    {
        SubscribeWaypoints();

        currentWaypoint = 0;
        ActivateArrow(waypoints[currentWaypoint].FrontPoint);
    }

    void OnDisable()
    {
        UnsubscribeWaypoints();
        DeactivateArrow();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (i < waypoints.Count - 1)
            {
                GizmosExtend.DrawArrow(position: waypoints[i].Transform.position, 
                                       direction: waypoints[i + 1].Transform.position - waypoints[i].Transform.position);
            }
            else
            {
                break;
            }
        }
        Gizmos.color = Color.white;
    }

    void OnPlayerWentThrowWaypoint()
    {
        currentWaypoint = Mathf.Clamp(currentWaypoint + 1, 0, waypoints.Count - 1);

        ActivateArrow(waypoints[currentWaypoint].FrontPoint);

        if (currentWaypoint == waypoints.Count)
        {
            DeactivateArrow();
            onPathFinished?.Invoke();
            
            gameObject.SetActive(false);
        }

        Debug.Log("Player went throw waypoint");
    }

    void OnPlayerReturnedThrowWaypoint()
    {
        currentWaypoint = Mathf.Clamp(currentWaypoint - 1, 0, waypoints.Count - 1);

        if (currentWaypoint == 0)
        {
            ActivateArrow(waypoints[currentWaypoint].FrontPoint);
        }
        else
        {
            ActivateArrow(waypoints[currentWaypoint].BackPoint);
        }

        Debug.Log("Player returned throw waypoint");
    }

    void SubscribeWaypoints()
    {
        foreach (Waypoint waypoint in waypoints)
        {
            waypoint.onPlayerWentThrowWaypoint += OnPlayerWentThrowWaypoint;
            waypoint.onPlayerReturnedWaypoint += OnPlayerReturnedThrowWaypoint;
        }
    }

    void UnsubscribeWaypoints()
    {
        foreach (Waypoint waypoint in waypoints)
        {
            waypoint.onPlayerWentThrowWaypoint -= OnPlayerWentThrowWaypoint;
            waypoint.onPlayerReturnedWaypoint -= OnPlayerReturnedThrowWaypoint;
        }
    }

    void ActivateArrow(Transform target)
    {
        WaypointIndicatorController.Instance.SetTarget(target);
    }

    void DeactivateArrow()
    {
        WaypointIndicatorController.Instance.RemoveTarget();
    }
}
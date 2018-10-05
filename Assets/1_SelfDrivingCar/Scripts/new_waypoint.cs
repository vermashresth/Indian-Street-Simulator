using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityStandardAssets.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

/**
 * Created by Zaneris.
 * Follows the route of a WaypointCircuit at a constant speed.
 * Enables Waypoint based speed settings.
 * Enables invoke of method calls triggered by waypoints.
 **/
public class new_waypoint : MonoBehaviour
{
    public UnityEngine.Object circuitObject;
    public WaypointCircuit circuit;
    public float routeSpeed = 50f;
    public float lookAheadDistance = 100f;

    #region Varying Speed Variables
    public bool varyingSpeed = false;
    public float initialSpeed;
    public float rateOfChange = 1f;
    public float[] waypointSpeedFactors;
    #endregion

    #region Invoke Method Variables
    public bool invokeMethods = false;
    public bool[] invokeWaypointEnabled;
    public string[] invokeNames;
    public float[] invokeDelay;
    public UnityEngine.Object[] invokeObject;
    #endregion

    private float[] distances;
    private float progressDistance;
    private float currentSpeed;
    private int lastWaypoint;

    // Pull Waypoint data from circuit to set up our varying speed variables.
    public void InitializeSpeeds()
    {
        if (waypointSpeedFactors == null || waypointSpeedFactors.Length != circuit.Waypoints.Length)
        {
            waypointSpeedFactors = new float[circuit.Waypoints.Length];
            for (int i = 0; i < waypointSpeedFactors.Length; i++)
                waypointSpeedFactors[i] = 1f;
            initialSpeed = routeSpeed;
        }
    }

    // Pulling for invoke method variables.
    public void InitializeInvoke()
    {
        if (invokeObject == null || invokeObject.Length != circuit.Waypoints.Length)
        {
            invokeWaypointEnabled = new bool[circuit.Waypoints.Length];
            invokeNames = new string[circuit.Waypoints.Length];
            invokeDelay = new float[circuit.Waypoints.Length];
            invokeObject = new UnityEngine.Object[circuit.Waypoints.Length];
            for (int i = 0; i < invokeNames.Length; i++)
                invokeWaypointEnabled[i] = false;
            initialSpeed = routeSpeed;
        }
    }

    private void Start()
    {
        // Set up our distances, so we know which waypoint we're at.
        distances = new float[circuit.Waypoints.Length + 1];
        for (int i = 1; i < circuit.Waypoints.Length; i++)
            distances[i] = (circuit.Waypoints[i].position - circuit.Waypoints[i - 1].position).magnitude + distances[i - 1];
        distances[circuit.Waypoints.Length] = (circuit.Waypoints[circuit.Waypoints.Length - 1].position - circuit.Waypoints[0].position).magnitude + distances[circuit.Waypoints.Length - 1];

        ResetWaypointCircuit();
    }

    // Reset everything to the starting point.
    public void ResetWaypointCircuit()
    {
        progressDistance = 0;
        transform.position = circuit.Waypoints[0].position;
        transform.LookAt(circuit.GetRoutePoint(lookAheadDistance + 6.576f).position);
        if (varyingSpeed)
        {
            currentSpeed = initialSpeed;
        }
        else
        {
            currentSpeed = routeSpeed;
        }
    }

    private void Update()
    {
        #region Determining our position relative to waypoints.
        if (progressDistance > distances[distances.Length - 1])
            progressDistance -= distances[distances.Length - 1];
        for (int i = 1; i < distances.Length; i++)
            if (progressDistance < distances[i])
            {
                // Check if we've passed a new waypoint and whether or not it has a method call attached.
                if (i - 1 != lastWaypoint && invokeMethods && invokeWaypointEnabled[i - 1])
                    ((MonoBehaviour)invokeObject[i - 1]).Invoke(invokeNames[i - 1], invokeDelay[i - 1]);
                lastWaypoint = i - 1;
                break;
            }
        #endregion
        if (varyingSpeed)
        { // Adjust speed based on Editor provided data.
            float waypointSpeed = Mathf.Pow(routeSpeed, waypointSpeedFactors[lastWaypoint]) - .99f;
            float acceleration = Mathf.Pow(routeSpeed, rateOfChange);
            if (Mathf.Abs(currentSpeed - waypointSpeed) > acceleration / 50f)
                currentSpeed += (waypointSpeed < currentSpeed ? -1f : 1f) * acceleration * Time.deltaTime;
            else
                currentSpeed = waypointSpeed;
        }
        else
        {
            currentSpeed = routeSpeed;
        }

        Vector3 nextPosition, nextDelta;
        do
        { // Making sure our target point is far enough ahead on the route.
            progressDistance += Time.deltaTime * currentSpeed * .8f;
            nextPosition = circuit.GetRoutePoint(progressDistance).position;
            nextDelta = nextPosition - transform.position;
        } while (nextDelta.magnitude < 10f);

        nextDelta.Normalize(); // Set our direction vector to exactly 1 in magnitude.
        nextDelta *= currentSpeed; // Scale it back up to exactly our specified speed.
        transform.position += nextDelta * Time.deltaTime;
        transform.LookAt(circuit.GetRoutePoint(progressDistance + lookAheadDistance).position);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position);
            Gizmos.DrawWireSphere(circuit.GetRoutePosition(progressDistance), 1);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(new_waypoint))]
public class BetterWaypointEditor : Editor {
    List<String> monoBehaviours;
    int totalMethods;

    public override void OnInspectorGUI() {
        new_waypoint script = (new_waypoint)target;
        script.circuitObject = EditorGUILayout.ObjectField("Waypoint Circuit", script.circuitObject, typeof(WaypointCircuit), true);
        if(script.circuitObject != null) {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("-- Settings --", EditorStyles.boldLabel);
            script.circuit = (WaypointCircuit)script.circuitObject;
            script.routeSpeed = EditorGUILayout.FloatField("Speed In Units/Sec", script.routeSpeed);
            script.lookAheadDistance = EditorGUILayout.FloatField("Look Ahead Distance", script.lookAheadDistance);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("-- Varying Speed --", EditorStyles.boldLabel);
            if(GUILayout.Toggle(script.varyingSpeed, " Enable Varying Circuit Speeds?")) {
                script.varyingSpeed = true;
                script.InitializeSpeeds();
            } else {
                script.varyingSpeed = false;
            }
            if(script.varyingSpeed) {
                EditorGUILayout.Space();
                script.initialSpeed = EditorGUILayout.FloatField("Initial Speed In Units/Sec", script.initialSpeed);
                EditorGUILayout.Space();
                for(int i = 0; i < script.waypointSpeedFactors.Length; i++)
                    script.waypointSpeedFactors[i] = EditorGUILayout.Slider("Waypoint " + i.ToString("D3") + " Factor", script.waypointSpeedFactors[i], 0f, 2f);
                EditorGUILayout.Space();
                script.rateOfChange = EditorGUILayout.Slider("Rate Of Change Factor", script.rateOfChange, 0f, 2f);
                EditorGUILayout.HelpBox("Waypoint speed factors take effect as soon as you pass that point, "
                    + "and how quickly the change happens is determined by the 'Rate Of Change'."
                    + "\nThe speed factor is exponential. For example, 30 to the power of 2 is 900.", MessageType.Info);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("-- Invoke Methods --", EditorStyles.boldLabel);
            if(GUILayout.Toggle(script.invokeMethods, " Enable Invoke Method Calls At Waypoints?")) {
                script.invokeMethods = true;
                script.InitializeInvoke();
            } else {
                script.invokeMethods = false;
            }
            if(script.invokeMethods) {
                EditorGUILayout.Space();
                for(int i = 0; i < script.invokeWaypointEnabled.Length; i++) {
                    script.invokeWaypointEnabled[i] = EditorGUILayout.Toggle("Invoke At Waypoint " + i.ToString("D3") + "?", script.invokeWaypointEnabled[i]);
                    if(script.invokeWaypointEnabled[i]) {
                        script.invokeObject[i] = EditorGUILayout.ObjectField("Game Object Target", script.invokeObject[i], typeof(MonoBehaviour), true);
                        if(script.invokeObject[i] != null) {
                            Type type = script.invokeObject[i].GetType();
                            MethodInfo[] methods = type.GetMethods();
                            if(monoBehaviours == null)
                                FillList();
                            string[] names = new string[methods.Length - totalMethods + 1];
                            if(names.Length == 0) {
                                EditorGUILayout.HelpBox("You have no public methods in your game object."
                                        + "\nAdd 'public' in front of the method you wish to invoke.", MessageType.Error);
                            } else {
                                int l = 0;
                                for(int j = 0; j < methods.Length; j++)
                                    if(!monoBehaviours.Contains(methods[j].Name)) {
                                        names[l++] = methods[j].Name;
                                    }
                                int index = 0;
                                for(int j = 0; j < names.Length; j++)
                                    if(names[j] == script.invokeNames[i]) {
                                        index = j;
                                        break;
                                    }
                                index = EditorGUILayout.Popup("Method To Call", index, names);
                                script.invokeNames[i] = names[index];
                                float delay = EditorGUILayout.FloatField("Delay In Seconds", script.invokeDelay[i]);
                                script.invokeDelay[i] = delay < 0f ? 0f : delay;
                            }
                        }
                    }
                }
            }
        }
    }

    private void FillList() {
        totalMethods = 0;
        monoBehaviours = new List<string>();
        MethodInfo[] methods = typeof(MonoBehaviour).GetMethods();
        foreach(MethodInfo mi in methods) {
            monoBehaviours.Add(mi.Name);
            totalMethods++;
        }
    }
}
#endif
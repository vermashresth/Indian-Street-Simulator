using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bb: MonoBehaviour {

public Transform target, target2;
     Camera cam; 
Vector3 screenPos;
//var mesh;

  void Start ()
     {
         cam = GetComponent < Camera > ();
	    
     } 

  void Update ()
     {
         


         screenPos = cam.WorldToScreenPoint (target.position);
         Debug.Log ("target is" + screenPos.x + "  "+ screenPos.y+ "pixels from the left");
     }
void OnGUI ()
     {
         GUI.Box (new Rect (screenPos.x, Screen.height - screenPos.y, 300,300 ), "This is a box");
     }
}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpGizmo : MonoBehaviour
{
    public bool isStatic;
    public bool isReference;

    void Start() {
        if (isReference)
        {
            var jumpScript = FindObjectOfType<JumpTrajectory>();
            jumpScript.RegisterJumpGizmo(this);
        }
    }
}

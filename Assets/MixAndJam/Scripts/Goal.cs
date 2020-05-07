using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public SearchandReplay demo;
    /*void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log("Goal reached");
        demo.reachedGoal = true;

        //demo.Replay();

    }*/

    private void OnTriggerEnter2D (Collider2D other)
    {
        demo.reachedGoal = true;
    }
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateOverTime : MonoBehaviour
{
    private Vector3 rotation = new Vector3(0, 1, 0);
    

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(rotation);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class CustomizeUIController : MonoBehaviour
{
    public List<SkinnedMeshRenderer> maskRenderers;
    public Transform dogTransform;
    public Animator dogAnimator;

    private void Start()
    {
        dogAnimator.Play("DoggoRig|Doggo_Idle_Stand");
    }

    // Update is called once per frame
    void Update()
    {
        dogTransform.eulerAngles += new Vector3(0, 30, 0) * Time.deltaTime;
    }
}

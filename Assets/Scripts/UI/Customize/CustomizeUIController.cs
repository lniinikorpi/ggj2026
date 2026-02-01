using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomizeUIController : MonoBehaviour
{
    public List<SkinnedMeshRenderer> maskRenderers;
    public List<Button> maskButtons;
    public Transform dogTransform;
    public Animator dogAnimator;

    private void Start()
    {
        dogAnimator.Play("DoggoRig|Doggo_Idle_Stand");
        SaveData save = SaveSystem.LoadGame();

        int placeIndex = Mathf.Clamp(save.playerLeaderboardIndex, 0, Mathf.Max(0, maskButtons.Count - 1));

        for (int i = 0; i < maskButtons.Count; i++)
            maskButtons[i].interactable = i <= placeIndex;

        int selectedMask = Mathf.Clamp(save.selectedMaskMaterialIndex, 0, placeIndex - 1);
        if (maskButtons.Count > 0)
            maskButtons[selectedMask].onClick.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        dogTransform.eulerAngles += new Vector3(0, 30, 0) * Time.deltaTime;
    }
}

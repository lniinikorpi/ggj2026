using System;
using UnityEngine;
using UnityEngine.UI;

public class CustomizeUIButton : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private CustomizeUIController controller;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        controller.meshRenderer.material = material;
    }
}

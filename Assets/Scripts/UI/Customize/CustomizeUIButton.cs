using UnityEngine;
using UnityEngine.UI;

public class CustomizeUIButton : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private CustomizeUIController controller;
    [SerializeField] private int maskIndex;
    [SerializeField] private int materialIndex;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        controller.maskRenderers[maskIndex].material = material;
        for (int i = 0; i < controller.maskRenderers.Count; i++)
        {
            if(i == maskIndex)
            {
                controller.maskRenderers[i].gameObject.SetActive(true);
                continue;
            }
            controller.maskRenderers[i].gameObject.SetActive(false);
        }

        SaveData save = SaveSystem.LoadGame();
        save.selectedMaskIndex = maskIndex;
        save.selectedMaskMaterialIndex = materialIndex;
        SaveSystem.SaveGame(save);
    }
}

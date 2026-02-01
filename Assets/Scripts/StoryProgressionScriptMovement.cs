using UnityEngine;

public class StoryProgressionScriptMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] Collider collider;
    void Start()
    {
        collider = this.GetComponent<Collider>();
    }
    private void OnTriggerEnter(Collider other)
    {
        // Checks if the GameObject's name is exactly "Player"
        if (other.name == "Player")
        {
            Debug.Log("The object named Player entered.");
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

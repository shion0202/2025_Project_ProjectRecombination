using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class MaterialPropertyController : MonoBehaviour
{
    [TextArea(2, 5)]
    public string HowToUse = "For the effect to appear, Add the material(s) in this list as a Full Screen Pass Renderer Feature to the active Universal Renderer Data asset. For more details check out the Documentation.";
    [Tooltip("Drag and drop your materials here in the Inspector.")]   
    public List<Material> materials; // Drag and drop your materials here in the Inspector
    string propertyName = "_enabled"; // Name of the property in the shader

    private bool isEnabled = true; // Internal "Is Enabled" state

    private void OnValidate()
    {
        // Update the shader properties for all materials whenever the "Is Enabled" state changes in the Inspector
        UpdateShaderProperties();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            UpdateShaderProperties();
        }
    }

    private void OnEnable()
    {
        isEnabled = true;
        UpdateShaderProperties();
    }

    private void OnDisable()
    {
        isEnabled = false;
        UpdateShaderProperties();
    }

    private void UpdateShaderProperties()
    {
        if (materials != null && materials.Count > 0)
        {
            int value = isEnabled ? 1 : 0;
            foreach (var material in materials)
            {
                if (material != null)
                {
                    material.SetInt(propertyName, value);
                }
            }
        }
    }
}

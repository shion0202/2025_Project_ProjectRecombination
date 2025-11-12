using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class F3DWeaponRangeLightController : MonoBehaviour
    {
        public int ControllerID;
        public Light[] LightSources;
        float[] LightSourcesValues;

        //
        public MeshRenderer[] Lights;
        float[] LightValues;
            
        // Use this for initialization
        void Awake()
        {
            //
            if (LightSources.Length > 0)
            {              
                LightSourcesValues = new float[LightSources.Length];

                for (int i = 0; i < LightSources.Length; i++)
                    LightSourcesValues[i] = LightSources[i].intensity;
            }

            //
            if(Lights.Length > 0)
            {          
                LightValues = new float[Lights.Length];

                for (int i = 0; i < Lights.Length; i++)
                    LightValues[i] = Lights[i].material.GetFloat("_HDRGlow");
            }
        }

        public void SwitchLights(bool state)
        {
            if (state)
            {
                for (int i = 0; i < LightSources.Length; i++)
                    LightSources[i].intensity = LightSourcesValues[i];

                for (int i = 0; i < Lights.Length; i++)
                    Lights[i].material.SetFloat("_HDRGlow", LightValues[i]);
            }
            else
            {
                for (int i = 0; i < LightSources.Length; i++)
                    LightSources[i].intensity = 0.0f;

                for (int i = 0; i < Lights.Length; i++)
                    Lights[i].material.SetFloat("_HDRGlow", 0.0f);
            }
        }


    
    }
}
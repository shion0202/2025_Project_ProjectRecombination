using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script is for one target object
//i made it so for you it will be more simple to add to your game if 
//you need to put the dissolve effect on one object

namespace GDNoob.MyPackage
{
    public class startAnimationForOneObject : MonoBehaviour
    {
        public KeyCode KeyToStartDissolve = KeyCode.Space;
        public KeyCode KeyToStartAppear = KeyCode.W;

        //the target object renderer
        private Renderer targetRenderer;


        //all the monkeys matirials
        private Material[] targetMatirial;


        //speed of the dissolve
        public float dissolveRate = 0.0125f;
        public float refreshRate = 0.025f;


        // Start is called before the first frame update
        void Start()
        {
            //asigning the renderer variable to the renderer of object the script is on
            targetRenderer = this.GetComponent<Renderer>();
            //assigning the matirial variable to the matirial of the object the script is on
            targetMatirial = targetRenderer.materials;

            //if you want the script not to be one the object with the shader
            //just change the variables from private to public and assign the RENDERER in the inspector


        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyToStartDissolve))
            {
                //calling the dissolve function when pressing space
                StartCoroutine(dissolveCo1());
            }

            if (Input.GetKeyDown(KeyToStartAppear))
            {
                //calling the emerge function when pressing w
                StartCoroutine(dissolveCo11());
            }

        }

        //disolve function
        IEnumerator dissolveCo1()
        {
            if (targetMatirial[0].GetFloat("_visble_amount") < 1)
            {
                float counter = 0;
                while (targetMatirial[0].GetFloat("_visble_amount") <= 1)
                {
                    //decrease
                    counter += dissolveRate;
                    for (int i = 0; i < targetMatirial.Length; i++)
                    {
                        targetMatirial[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate);
                }
            }

        }
        //disolve function fot the other monkeys



        //emerge
        IEnumerator dissolveCo11()
        {
            if (targetMatirial[0].GetFloat("_visble_amount") > 0)
            {
                float counter = 1;
                while (counter >= 0) // Continue until counter reaches 1
                {
                    // Increase
                    counter -= dissolveRate; // Increase the counter by dissolveRate
                    for (int i = 0; i < targetMatirial.Length; i++)
                    {
                        targetMatirial[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate); // Wait before the next iteration
                }
            }
        }






    }
}
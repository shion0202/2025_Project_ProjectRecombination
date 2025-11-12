using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GDNoob.MyPackage
{
    public class startAnimationScript : MonoBehaviour
    {
        public KeyCode KeyToStartDissolve = KeyCode.Space;
        public KeyCode KeyToStartAppear = KeyCode.W;

        //all the monkeys renderer
        public Renderer monkeyRender1;
        public Renderer monkeyRender2;
        public Renderer monkeyRender3;

        //all the monkeys matirials
        public Material[] monkeyMaterial1;
        public Material[] monkeyMaterial2;
        public Material[] monkeyMaterial3;

        //speed of the dissolve
        public float dissolveRate = 0.0125f;
        public float refreshRate = 0.025f;


        // Start is called before the first frame update
        void Start()
        {
            //asigning each matirial to its renderer
            monkeyMaterial1 = monkeyRender1.materials;
            monkeyMaterial2 = monkeyRender2.materials;
            monkeyMaterial3 = monkeyRender3.materials;

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyToStartDissolve))
            {
                //calling the dissolve function when pressing space
                StartCoroutine(dissolveCo1());
                StartCoroutine(dissolveCo2());
                StartCoroutine(dissolveCo3());
            }

            if (Input.GetKeyDown(KeyToStartAppear))
            {
                //calling the emerge function when pressing w
                StartCoroutine(dissolveCo11());
                StartCoroutine(dissolveCo22());
                StartCoroutine(dissolveCo33());
            }

        }

        //disolve function
        IEnumerator dissolveCo1()
        {
            if (monkeyMaterial1[0].GetFloat("_visble_amount") < 1)
            {
                float counter = 0;
                while (monkeyMaterial1[0].GetFloat("_visble_amount") <= 1)
                {
                    //decrease
                    counter += dissolveRate;
                    for (int i = 0; i < monkeyMaterial1.Length; i++)
                    {
                        monkeyMaterial1[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate);
                }
            }

        }
        //disolve function fot the other monkeys
        IEnumerator dissolveCo2()
        {
            if (monkeyMaterial2[0].GetFloat("_visble_amount") < 1)
            {
                float counter = 0;
                while (monkeyMaterial2[0].GetFloat("_visble_amount") <= 1)
                {
                    //decrease
                    counter += dissolveRate;
                    for (int i = 0; i < monkeyMaterial2.Length; i++)
                    {
                        monkeyMaterial2[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate);
                }
            }
        }

        IEnumerator dissolveCo3()
        {
            if (monkeyMaterial3[0].GetFloat("_visble_amount") < 1)
            {
                float counter = 0;
                while (monkeyMaterial3[0].GetFloat("_visble_amount") <= 1)
                {
                    //decrease
                    counter += dissolveRate;
                    for (int i = 0; i < monkeyMaterial3.Length; i++)
                    {
                        monkeyMaterial3[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate);
                }
            }
        }


        //emerge
        IEnumerator dissolveCo11()
        {
            if (monkeyMaterial1[0].GetFloat("_visble_amount") > 0)
            {
                float counter = 1;
                while (counter >= 0) // Continue until counter reaches 1
                {
                    // Increase
                    counter -= dissolveRate; // Increase the counter by dissolveRate
                    for (int i = 0; i < monkeyMaterial1.Length; i++)
                    {
                        monkeyMaterial1[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate); // Wait before the next iteration
                }
            }
        }
        //emerge for the other monkeys
        IEnumerator dissolveCo22()
        {
            if (monkeyMaterial2[0].GetFloat("_visble_amount") > 0)
            {
                float counter = 1;
                while (counter >= 0) // Continue until counter reaches 1
                {
                    // Increase
                    counter -= dissolveRate; // Increase the counter by dissolveRate
                    for (int i = 0; i < monkeyMaterial2.Length; i++)
                    {
                        monkeyMaterial2[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate); // Wait before the next iteration
                }
            }
        }

        IEnumerator dissolveCo33()
        {
            if (monkeyMaterial3[0].GetFloat("_visble_amount") > 0)
            {
                float counter = 1;
                while (counter >= 0) // Continue until counter reaches 1
                {
                    // Increase
                    counter -= dissolveRate; // Increase the counter by dissolveRate
                    for (int i = 0; i < monkeyMaterial3.Length; i++)
                    {
                        monkeyMaterial3[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate); // Wait before the next iteration
                }
            }
        }

    }
}
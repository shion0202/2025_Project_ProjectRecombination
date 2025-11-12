using UnityEngine;
using UnityEditor;
using System;

namespace ArcadeVP
{
    public class ArcadeVehicleCreator : EditorWindow
    {

        ArcadeVehicleController preset;
        Transform VehicleParent;
        Transform wheelFL;
        Transform wheelFR;
        Transform wheelRL;
        Transform wheelRR;

        MeshRenderer bodyMesh;
        MeshRenderer wheelMesh;

        private GameObject NewVehicle;

        private const string DiscordUrl = "https://discord.gg/yU82FbNHcu";
        private const string TutorilUrl = "https://youtu.be/2Sif8yKKl40?si=ZWJkGdyLkn4o0IHl";
        private const string DocumentationUrl = "/Ash Assets/Arcade Vehicle Physics/Documentation/Documentation.pdf";
        private const string AshDevAssets = "https://assetstore.unity.com/publishers/52093";


        [MenuItem("Tools/Arcade Vehicle Physics")]

        static void OpenWindow()
        {
            ArcadeVehicleCreator vehicleCreatorWindow = (ArcadeVehicleCreator)GetWindow(typeof(ArcadeVehicleCreator));
            vehicleCreatorWindow.minSize = new Vector2(400, 500);
            vehicleCreatorWindow.Show();
        }

        private void OnGUI()
        {
            DrawHelpAndTutorialsSection();

            DrawVehicleCreation();

        }

        private void DrawHelpAndTutorialsSection()
        {
            // Custom styles
            var sectionStyle = new GUIStyle("Box")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.2f, 0.6f, 1f) }
            };

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(35, 10, 5, 5)
            };

            EditorGUILayout.BeginVertical(sectionStyle);

            // Header
            EditorGUILayout.LabelField("Help & Tutorials", headerStyle);
            EditorGUILayout.Space(5);

            // First Row
            EditorGUILayout.BeginHorizontal();
            {
                // Documentation Button
                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
                if (GUILayout.Button(new GUIContent("  Documentation", EditorGUIUtility.IconContent("_Help").image),
                    buttonStyle, GUILayout.Height(28)))
                {
                    string doc_path = Application.dataPath + DocumentationUrl;
                    Application.OpenURL("file://" + doc_path);
                }

                // Tutorial Button
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
                if (GUILayout.Button(new GUIContent("  Video Tutorials", EditorGUIUtility.IconContent("VideoPlayer Icon").image),
                    buttonStyle, GUILayout.Height(28)))
                {
                    Application.OpenURL(TutorilUrl);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Second Row
            EditorGUILayout.BeginHorizontal();
            {
                // Discord Button
                GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
                if (GUILayout.Button(new GUIContent("  Join Discord", EditorGUIUtility.IconContent("BuildSettings.Web.Small").image),
                    buttonStyle, GUILayout.Height(28)))
                {
                    Application.OpenURL(DiscordUrl);
                }

                // More Ash Dev Assets Button
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
                if (GUILayout.Button(new GUIContent("  Ash Dev Assets", EditorGUIUtility.IconContent("AssetStore Icon").image),
                    buttonStyle, GUILayout.Height(28)))
                {
                    Application.OpenURL(AshDevAssets);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white; // Reset background color
            EditorGUILayout.EndVertical();
        }

        private void DrawVehicleCreation()
        {
            // Styles
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                margin = new RectOffset(5, 5, 10, 10)
            };

            var sectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            var sectionStyleFull = new GUIStyle("Box")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
                margin = new RectOffset(5, 5, 8, 8)
            };

            EditorGUILayout.BeginVertical(sectionStyleFull);

            // Main Layout
            EditorGUILayout.BeginVertical();

            // Title
            headerStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Arcade Vehicle Creator", headerStyle);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            // Preset Section
            EditorGUILayout.BeginVertical(sectionStyle);
            {
                preset = EditorGUILayout.ObjectField("Vehicle Preset", preset,
                    typeof(ArcadeVehicleController), true) as ArcadeVehicleController;
            }
            EditorGUILayout.EndVertical();

            // Vehicle Setup Section
            EditorGUILayout.BeginVertical(sectionStyle);
            {
                // Vehicle Parent
                VehicleParent = EditorGUILayout.ObjectField("Vehicle Parent", VehicleParent,
                    typeof(Transform), true) as Transform;

                // Wheels Group
                EditorGUILayout.LabelField("Wheels", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                wheelFL = EditorGUILayout.ObjectField("Front Left", wheelFL,
                    typeof(Transform), true) as Transform;
                wheelFR = EditorGUILayout.ObjectField("Front Right", wheelFR,
                    typeof(Transform), true) as Transform;
                wheelRL = EditorGUILayout.ObjectField("Rear Left", wheelRL,
                    typeof(Transform), true) as Transform;
                wheelRR = EditorGUILayout.ObjectField("Rear Right", wheelRR,
                    typeof(Transform), true) as Transform;
                EditorGUI.indentLevel--;

            }
            EditorGUILayout.EndVertical();


            if (GUILayout.Button("Create Vehicle", buttonStyle))
            {
                createVehicle();
            }

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            // Mesh Setup Section
            EditorGUILayout.BeginVertical(sectionStyle);
            {
                bodyMesh = EditorGUILayout.ObjectField("Body Mesh", bodyMesh,
                    typeof(MeshRenderer), true) as MeshRenderer;
                wheelMesh = EditorGUILayout.ObjectField("Wheel Mesh", wheelMesh,
                    typeof(MeshRenderer), true) as MeshRenderer;
            }
            EditorGUILayout.EndVertical();


            if (GUILayout.Button("Adjust Colliders", buttonStyle))
            {
                adjustColliders();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void adjustColliders()
        {
            if (NewVehicle.GetComponent<BoxCollider>())
            {
                NewVehicle.GetComponent<BoxCollider>().center = Vector3.zero;
                NewVehicle.GetComponent<BoxCollider>().size = bodyMesh.bounds.size;
            }

            if (NewVehicle.GetComponent<CapsuleCollider>())
            {
                NewVehicle.GetComponent<CapsuleCollider>().center = Vector3.zero;
                NewVehicle.GetComponent<CapsuleCollider>().height = bodyMesh.bounds.size.z;
                NewVehicle.GetComponent<CapsuleCollider>().radius = bodyMesh.bounds.size.x / 2;

            }

            Vector3 SpheareRBOffset = new Vector3(NewVehicle.transform.position.x,
                                                  wheelFL.position.y + bodyMesh.bounds.extents.y - wheelMesh.bounds.size.y / 2,
                                                  NewVehicle.transform.position.z);

            NewVehicle.GetComponent<ArcadeVehicleController>().skidWidth = wheelMesh.bounds.size.x / 2;
            if (NewVehicle.transform.Find("SphereRB"))
            {
                NewVehicle.transform.Find("SphereRB").GetComponent<SphereCollider>().radius = bodyMesh.bounds.extents.y;
                NewVehicle.transform.Find("SphereRB").position = SpheareRBOffset;
            }

            NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("Skid marks FL").position = wheelFL.position - Vector3.up * (wheelMesh.bounds.size.y / 2 - 0.02f);
            NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("Skid marks FR").position = wheelFR.position - Vector3.up * (wheelMesh.bounds.size.y / 2 - 0.02f);
            NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("Skid marks RL").position = wheelRL.position - Vector3.up * (wheelMesh.bounds.size.y / 2 - 0.02f);
            NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("Skid marks RR").position = wheelRR.position - Vector3.up * (wheelMesh.bounds.size.y / 2 - 0.02f);

        }

        private void createVehicle()
        {
            Make_Vehicle_Ready_For_Setup();

            NewVehicle = Instantiate(preset.gameObject, bodyMesh.bounds.center, VehicleParent.rotation);
            NewVehicle.name = "Arcade_" + VehicleParent.name;
            GameObject.DestroyImmediate(NewVehicle.transform.Find("Mesh").Find("Body").GetChild(0).gameObject);
            if (NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFL"))
            {
                GameObject.DestroyImmediate(NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFL").Find("WheelFL Axel").GetChild(0).gameObject);
            }
            if (NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFR"))
            {
                GameObject.DestroyImmediate(NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFR").Find("WheelFR Axel").GetChild(0).gameObject);
            }
            if (NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRL"))
            {
                GameObject.DestroyImmediate(NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRL").Find("WheelRL Axel").GetChild(0).gameObject);
            }
            if (NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRR"))
            {
                GameObject.DestroyImmediate(NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRR").Find("WheelRR Axel").GetChild(0).gameObject);
            }

            VehicleParent.parent = NewVehicle.transform.Find("Mesh").Find("Body");

            NewVehicle.transform.Find("Mesh").transform.Find("Wheels").position = VehicleParent.position;

            if (NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFL"))
            {
                NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFL").position = wheelFL.position;
                wheelFL.parent = NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFL").Find("WheelFL Axel");
            }
            if (NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFR"))
            {
                NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFR").position = wheelFR.position;
                wheelFR.parent = NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelFR").Find("WheelFR Axel");
            }
            if (NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRL"))
            {
                NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRL").position = wheelRL.position;
                wheelRL.parent = NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRL").Find("WheelRL Axel");
            }
            if (NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRR"))
            {
                NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRR").position = wheelRR.position;
                wheelRR.parent = NewVehicle.transform.Find("Mesh").transform.Find("Wheels").Find("WheelRR").Find("WheelRR Axel");
            }

        }

        private void Make_Vehicle_Ready_For_Setup()
        {

            var AllVehicleColliders = VehicleParent.GetComponentsInChildren<Collider>();
            foreach (var collider in AllVehicleColliders)
            {
                DestroyImmediate(collider);
            }

            var AllRigidBodies = VehicleParent.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in AllRigidBodies)
            {
                DestroyImmediate(rb);
            }

        }
    }
}

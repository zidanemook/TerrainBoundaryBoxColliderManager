using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PixelCrushers.SceneStreamer;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class Node
{
    public Terrain terrain;
    public List<Node> neighbors;

    public Node(Terrain terrain)
    {
        this.terrain = terrain;
        neighbors = new List<Node>();
    }
}

public class TerrainBoundaryBoxColliderManager : EditorWindow
{ 
    private float terrainSize = 1000f; 
    private float triggerWidth = 100f;
    private List<string> acceptedTags = new List<string>();
    
    [MenuItem("Window/TerrainConnector")] 
    
    public static void ShowWindow()
       {
           GetWindow<TerrainBoundaryBoxColliderManager>("TerrainBoundaryBoxColliderManager");
       }
       
       void OnGUI()
       {
           GUILayout.Label("This function receives selected terrains and deploys\n a trigger on each terrain's border\n if it has a neighbor.");
           GUILayout.Label("1. Select Terrain");
           GUILayout.Label("2. Input Terrain size");
           GUILayout.Label("3. Input Collider Thickness");
           GUILayout.Label("4. Select Tags for trigger");
           GUILayout.Label("5. Push the button");

           EditorGUILayout.LabelField("Enter Terrain Size:");
           terrainSize = EditorGUILayout.FloatField(terrainSize);
           
           EditorGUILayout.LabelField("Enter Collider Thickness");
           triggerWidth = EditorGUILayout.FloatField(triggerWidth);
           
           GUILayout.Label("Accepted Tags:");
           string[] availableTags = UnityEditorInternal.InternalEditorUtility.tags;
           

           for (int i = 0; i < acceptedTags.Count; i++)
           {
               int tagIndex = Array.IndexOf(availableTags, acceptedTags[i]);
               int newTagIndex = EditorGUILayout.Popup("Tag", tagIndex, availableTags);
               acceptedTags[i] = availableTags[newTagIndex];
           }

           if (GUILayout.Button("Add Tag"))
           {
               acceptedTags.Add(availableTags[0]);
           }

           if (GUILayout.Button("Remove Tag"))
           {
               if (acceptedTags.Count > 0)
               {
                   acceptedTags.RemoveAt(acceptedTags.Count - 1);
               }
           }
           
           GUILayout.Label("\n");
           GUILayout.Label("\n");
           
           if (GUILayout.Button("Connect Terrains") && terrainSize > 0)
           {
               ConnectSelectedTerrains();
           }
       }

       private void ConnectSelectedTerrains()
       {
           GameObject[] selectedObjects = Selection.gameObjects;
           List<Terrain> terrainlist = new List<Terrain>();
           
           foreach (GameObject go in selectedObjects)
           {
               Terrain terrain = go.GetComponent<Terrain>();
               if (terrain != null)
               {
                   terrainlist.Add(terrain);
               }
           }

           Scene currentScene = SceneManager.GetActiveScene();

           foreach (Terrain terrain in terrainlist)
           {

               Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
               SceneManager.SetActiveScene(newScene);
               
               GameObject sceneRoot = new GameObject(terrain.name);
               SceneManager.MoveGameObjectToScene(sceneRoot, newScene);

               GameObject copyOfTerrain = Instantiate(terrain.gameObject);
               SceneManager.MoveGameObjectToScene(copyOfTerrain, newScene);

               float halfTerrainSize = terrainSize * 0.5f;
               float halfTriggerWidth = triggerWidth * 0.5f;

               Vector3 direction = Vector3.zero;
               Terrain neighbor = null;
               for (int i = 0; i < 4; ++i)
               {
                   if (i == 0 && terrain.topNeighbor)
                   {
                       direction = Vector3.forward;
                       neighbor = terrain.topNeighbor;
                   }
                   else if (i == 1 && terrain.rightNeighbor)
                   {
                       direction = Vector3.right;
                       neighbor = terrain.rightNeighbor;
                   }
                   else if (i == 2 && terrain.bottomNeighbor)
                   {
                       direction = Vector3.back;
                       neighbor = terrain.bottomNeighbor;
                   }
                   else if (i == 3 && terrain.leftNeighbor)
                   {
                       direction = Vector3.left;
                       neighbor = terrain.leftNeighbor;
                   }
                   else
                       continue;
                   

                   Vector3 center = terrain.transform.position + Vector3.one * halfTerrainSize;
                       
                   // Trigger's position at boarder
                   Vector3 position = new Vector3(center.x + (Math.Abs(direction.x) > 0f ? direction.x * (halfTerrainSize - halfTriggerWidth) : 0f), 0f, center.z + (Math.Abs(direction.z) > 0f ? direction.z * (halfTerrainSize - halfTriggerWidth)  : 0f));
                           
                   GameObject triggerObject = new GameObject("TriggerTo" + neighbor.name);
                   triggerObject.transform.position = position;
                   triggerObject.transform.SetParent(sceneRoot.transform);
                       
                   Vector3 scale;
                   if (direction == Vector3.right || direction == Vector3.left)
                   {
                       scale = new Vector3(triggerWidth, 100000, terrainSize);
                   }
                   else // 위 또는 아래
                   {
                       scale = new Vector3(terrainSize, 100000, triggerWidth);
                   }
                   //triggerObject.transform.localScale = scale;

                   // BoxCollider 컴포넌트를 추가합니다.
                   BoxCollider collider = triggerObject.AddComponent<BoxCollider>();
                   collider.size = scale;
                   collider.isTrigger = true;
                   
                   // SceneEdge 스크립트를 추가하고 변수를 설정합니다.
                   SceneEdge edgeScript = triggerObject.AddComponent<SceneEdge>();
                   edgeScript.currentSceneRoot = sceneRoot; // 씬 설정
                   edgeScript.nextSceneName = neighbor.name; // 이웃 터레인 이름 설정
                   edgeScript.acceptedTags = acceptedTags.ToList();
               }

               string sceneName = terrain.name;
               string scenePath = "Assets/Scenes/" + sceneName + ".unity";
               EditorSceneManager.SaveScene(newScene, scenePath);
               
               SceneManager.SetActiveScene(currentScene);
           }
       }
}

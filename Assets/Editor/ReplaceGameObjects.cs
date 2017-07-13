using UnityEngine;
using UnityEditor;
using System.Collections;

/*
 * http://forum.unity3d.com/threads/24311-Replace-game-object-with-prefab/page2
 * */

public class ReplaceGameObjects : ScriptableWizard

{
	public GameObject useGameObject;

	[MenuItem ("Custom/Replace GameObjects")]  

	static void CreateWizard ()
	{
		ScriptableWizard.DisplayWizard("Replace GameObjects", typeof(ReplaceGameObjects), "Replace");
	}

	void OnWizardCreate ()
	{
		foreach (Transform t in Selection.transforms)
		{
			GameObject newObject = PrefabUtility.InstantiatePrefab(useGameObject) as GameObject;
			Transform newT = newObject.transform;

			newT.position = t.position;
			newT.rotation = t.rotation;
			newT.localScale = t.localScale;
			newT.parent = t.parent;

		}

		foreach (GameObject go in Selection.gameObjects)
		{
			DestroyImmediate(go);
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using TooSimpleFramework.UI;
using UnityEngine;
using UnityEngine.UI;

public class OutlineTrans : MonoBehaviour
{
	[SerializeField]
	Material M;

	[ContextMenu("Go")]
	public void Go()
	{
		DeepForeachChildsCompoent<Outline>(transform, (Outline t) =>
		{
			//t.gameObject.GetComponent<Text>().material = M;
			t.gameObject.AddComponent<OutlineEx>();//.OutlineColor = new Color(0, 0, 0, 0.5f);
			DestroyImmediate(t);
		});
	}

	[ContextMenu("GoBack")]
	public void GoBack()
	{
		DeepForeachChildsCompoent<OutlineEx>(transform, (OutlineEx t) =>
		{
			t.gameObject.GetComponent<Text>().material = null;
			t.gameObject.AddComponent<Outline>();//.OutlineColor = new Color(0, 0, 0, 0.5f);
			DestroyImmediate(t);
		});
	}

	private void DeepForeachChildsCompoent<T>(Transform t, Action<T> Find) where T : Component
	{
		foreach (Transform child in t)
		{

			if (child == t)
				continue;

			T target = child.GetComponent<T>();
			if (target != null)
				Find(target);

			DeepForeachChildsCompoent<T>(child, Find);
		}
	}
}

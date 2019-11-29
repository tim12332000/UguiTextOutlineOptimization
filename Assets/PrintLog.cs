using UnityEngine;
using System.Collections;
public class PrintLog : MonoBehaviour
{
	string myLog;
	Queue myLogQueue = new Queue();
	void Start()
	{
		Debug.Log("Im Log");
		Debug.LogWarning("Im Warning Log");
		Debug.LogError("Im Error Log");
	}
	void OnEnable()
	{
		Application.logMessageReceived += HandleLog;
	}
	void OnDisable()
	{
		Application.logMessageReceived -= HandleLog;
	}
	void HandleLog(string logString, string stackTrace, LogType type)
	{
		myLog = logString;
		string newString = "\n [" + type + "] : " + myLog;
		switch (type)
		{
			case LogType.Error:
			case LogType.Exception:
				newString = "" + newString + "";
				break;
			case LogType.Warning:
				newString = "" + newString + "";
				break;
			default:
				newString = "" + newString + "";
				break;
		}
		myLogQueue.Enqueue(newString);
		if (type == LogType.Exception)
		{
			newString = "" + "\n" + stackTrace + "";
			myLogQueue.Enqueue(newString);
		}
		myLog = string.Empty;
		foreach (string mylog in myLogQueue)
		{
			myLog += mylog;
		}
	}
	void OnGUI()
	{
		GUILayout.Label(myLog);
	}
}

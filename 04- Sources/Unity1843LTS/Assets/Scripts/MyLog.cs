using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MyLog : MonoBehaviour
{
    public Text UILogText;
    string myLog;
    Queue myLogQueue = new Queue();

    void Start()
    {
        Debug.Log("Log1");
        Debug.Log("Log2");
        Debug.Log("Log3");
        Debug.Log("Log4");
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
        myLogQueue.Enqueue(newString);
        if (type == LogType.Exception)
        {
            newString = "\n" + stackTrace;
            myLogQueue.Enqueue(newString);
        }
        myLog = string.Empty;
        foreach (string mylog in myLogQueue)
        {
            myLog += mylog;
        }
    }

    void Update()
    {
        //GUILayout.Label(myLog);

        //GUI.Label(new Rect(5, // x, left offset
        //            (Screen.height - 150), // y, bottom offset
        //            300f, // width
        //            150f), myLog, GUI.skin.textArea); // height, text, Skin features}
        UILogText.text = myLog;

    }
}
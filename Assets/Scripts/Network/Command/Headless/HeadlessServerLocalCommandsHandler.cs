using System.Net;
using System.Threading;
using Mirror;
using UnityEngine;

public class HeadlessServerLocalCommands : MonoBehaviour
{
    private HttpListener httpListener;
    private Thread listenerThread;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        listenerThread = new Thread(StartListener);
        listenerThread.Start();
    }

    private void OnApplicationQuit()
    {
        httpListener?.Stop();
        listenerThread?.Join();
    }

    private void StartListener()
    {
        //var configParams = GameNetworkManager.Singleton.serverConfig.GetConfigParams();
        //if (configParams.headlessLocalCommandsPort <= 0)
        //{
        //    return;
        //}
        //if (configParams.port == configParams.headlessLocalCommandsPort || configParams.headlessLocalCommandsPort == configParams.officialServerApiPort)
        //{
        //    Debug.Log($"HTTP local commands port conflicts with other ports in the config. Cannot start local commands server.");
        //    return;
        //}
        //httpListener = new HttpListener();
        //httpListener.Prefixes.Add($"http://127.0.0.1:{configParams.headlessLocalCommandsPort}/");
        //httpListener.Start();

        //Debug.Log($"HTTP local commands server started on http://127.0.0.1:{configParams.headlessLocalCommandsPort}");

        //while (true)
        //{
        //    HttpListenerContext context = httpListener.GetContext();
        //    HttpListenerRequest request = context.Request;
        //    HttpListenerResponse response = context.Response;

        //    string command = WebUtility.UrlDecode(request.RawUrl.Trim('/'));
        //    var output = ProcessCommand(command, out var logOutput);

        //    Debug.Log($"Local command received: input: {command}; output: {logOutput}");

        //    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(output);
        //    response.ContentLength64 = buffer.Length;
        //    response.OutputStream.Write(buffer, 0, buffer.Length);
        //    response.OutputStream.Close();
        //}
    }

    private string ProcessCommand(string command, out string logOutput)
    {
        if (NetworkServer.active && NetworkCommandsProcessor.Singleton)
            return NetworkCommandsProcessor.Singleton.ServerProceedCommand(sender: null, input: command, out logOutput);
        logOutput = "Error: cannot access commands processor";
        return logOutput;
    }
}
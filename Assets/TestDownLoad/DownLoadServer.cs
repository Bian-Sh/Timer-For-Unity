
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Timer = QFramework.TimeExtend.Timer;
using QFramework.TimeExtend;
public class DownLoadServer : MonoBehaviour
{
    private HttpListener server;
    public int port = 8083; //下载专用端口
    public Text text;
    private volatile bool serverEnabled = true;
    private void Awake()
    {
        Timer.IntializeDriver();
    }

    public void OnEnable()
    {

        server = new HttpListener();
        server.Prefixes.Add("http://localhost:" + port + "/");
        server.Start();
        serverEnabled = true;
        new Thread(ListenThread).Start();
    }

    private void ListenThread()
    {
        while (serverEnabled)
        {
            var context = server.GetContext();
            new Thread(ResponseThread).Start(context);
        }
    }

    private void ResponseThread(object obj)
    {
        var context = (HttpListenerContext)obj;

        //		Debug.Log("request for " + context.Request.Url.AbsolutePath);
        var res = context.Response;
        res.StatusCode = 200;
        var output = new StreamWriter(res.OutputStream);
        var path = context.Request.Url.AbsolutePath;
        Timer.AddTimer(0).OnCompleted(()=> 
        {
            text.text = path;
        });

        switch (path)
        {
                 case "/bigFile":
                {
                    var str = "Lorem ipsum dolor sit amet.\n";
                    long count = 1024 * 1024 * 100;

                    res.AddHeader("Content-length", (str.Length * count).ToString());
                    res.AddHeader("Content-type", "application/octet-stream");

                    //For speed, prep a buffer to bulk move from.
                    var strBytes = Encoding.ASCII.GetBytes(str);
                    var buf = new byte[1024 * strBytes.Length];
                    for (int i = 0; i < 1024; i++) Array.Copy(strBytes, 0, buf, i * strBytes.Length, strBytes.Length);

                    //Send data
                    for (int i = 0; i < count / 1024; i++) res.OutputStream.Write(buf, 0, buf.Length);
                    break;
                }

            case "/slowFile":

            case "/slowPage":
                {
                    var str = "Lorem ipsum dolor sit amet.\n";
                    var count = 1024 * 1024;

                    res.AddHeader("Content-length", (str.Length * count).ToString());
                    res.AddHeader("Content-type", path == "/slowFile" ? "application/octet-stream" : "text/plain");

                    for (int i = 0; i < count; i++)
                    {
                        output.Write(str);
                        Thread.Sleep(1);
                    }
                    break;
                }
            default:
                context.Response.StatusCode = 404;
                output.Write("Not found");
                break;
        }

        output.Close();
    }

    public void OnDisable()
    {
        serverEnabled = false;
        server.Stop();
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Timer = QFramework.TimeExtend.Timer;
using QFramework.TimeExtend;
using System;
using System.IO;
using UnityEngine.UI;

public class TestDownload : MonoBehaviour
{
    public string url = "http://localhost:8083/bigFile";
    public string savePath = "";
    public Text finish;
    public Text update;

    HttpDownLoader DownLoader;
    void Awake()
    {
        savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestDownLoad");
        Timer.IntializeDriver();
        Loom.Initialize();
        DownLoader = new HttpDownLoader();
        DownLoader.OnDownLoadCompleted.AddListener(() =>
               {
                   this.finish.text = "下载完成！";
               });
        DownLoader.OnDownLoadUpdate.AddListener(v =>
        {
            this.update.text = string.Format("下载进度：{0} %", (v * 100).ToString("f2"));
        });

    }
    private void Start()
    {
        this.DownLoader.DownLoad(url, savePath);
    }

    private void OnDisable()
    {
        this.DownLoader.Close();
    }
}

using System.IO;
using System.Net;
using System.Threading;
using Timer = QFramework.TimeExtend.Timer;
using QFramework.TimeExtend;
using UnityEngine.Events;

public class HttpDownLoader
{
    public class DownloadEvent<T> : UnityEvent<T> { }
    public UnityEvent OnDownLoadCompleted = new UnityEvent(); //下载完成的回调
    public DownloadEvent<float> OnDownLoadUpdate = new DownloadEvent<float>();  //下载实时进度回调

    private bool isStop; //涉及子线程要注意,Unity关闭的时候子线程不会关闭，所以要有一个标识

    private Thread thread;  //子线程负责下载，否则会阻塞主线程，Unity界面会卡主

    public bool isDone { get; private set; } = false; //表示下载是否完成


    /// <summary>
    /// 下载方法(断点续传)
    /// </summary>
    /// <param name="url">URL下载地址</param>
    /// <param name="savePath">Save path保存路径</param>
    /// <param name="callBack">Call back回调函数</param>
    public void DownLoad(string url, string savePath)
    {
        isStop = false;
        Timer.ShowLog = false;
        ThreadStart DownLoadHandler = () =>
         {
             //判断保存路径是否存在
             if (!Directory.Exists(savePath))
             {
                 Directory.CreateDirectory(savePath);
             }
             //这是要下载的文件名，比如从服务器下载a.zip到D盘，保存的文件名是test
             string filePath = savePath + "/test";
             float progress = 0;

             //使用流操作文件
             FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
             //获取文件现在的长度
             long fileLength = fs.Length;
             //获取下载文件的总长度
             long totalLength = GetLength(url);

             //如果没下载完
             if (fileLength < totalLength)
             {
                 //断点续传核心，设置本地文件流的起始位置
                 fs.Seek(fileLength, SeekOrigin.Begin);

                 HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

                 //断点续传核心，设置远程访问文件流的起始位置
                 request.AddRange((int)fileLength);
                 Stream stream = request.GetResponse().GetResponseStream();

                 byte[] buffer = new byte[1024];
                 //使用流读取内容到buffer中
                 //注意方法返回值代表读取的实际长度,并不是buffer有多大，stream就会读进去多少
                 int length = stream.Read(buffer, 0, buffer.Length);
                 while (length > 0)
                 {
                     //如果Unity客户端关闭，停止下载
                     if (isStop) break;
                     //将内容再写入本地文件中
                     fs.Write(buffer, 0, length);
                     //计算进度
                     fileLength += length;
                     progress = (float)fileLength / (float)totalLength;
                     //Loom.QueueOnMainThread((v) =>
                     //{
                     //    OnDownLoadUpdate?.Invoke(progress);
                     //},null);
                     Timer.AddTimer(0, "ForUpdateEvent", true).OnCompleted(() =>
                      {
                          OnDownLoadUpdate?.Invoke(progress);
                      });
                     //类似尾递归
                     length = stream.Read(buffer, 0, buffer.Length);
                 }
                 stream.Close();
                 stream.Dispose();

             }
             else
             {
                 progress = 1;
             }
             fs.Close();
             fs.Dispose();
             //如果下载完毕，执行回调
             if (progress == 1)
             {
                 isDone = true;
                 Timer.DelTimer("ForUpdateEvent");
                 Timer.AddTimer(0, "ForOnCompleteEvent").OnCompleted(() =>
                 {
                     OnDownLoadCompleted?.Invoke();
                 });
             }
         };
        thread = new Thread(DownLoadHandler);//开启子线程下载,使用匿名方法
        thread.IsBackground = true;//开启子线程
        thread.Start();
    }


    /// <summary>
    /// 获取下载文件的大小
    /// </summary>
    /// <returns>The length.</returns>
    /// <param name="url">URL.</param>
    long GetLength(string url)
    {
        HttpWebRequest requet = HttpWebRequest.Create(url) as HttpWebRequest;
        requet.Method = "HEAD";
        HttpWebResponse response = requet.GetResponse() as HttpWebResponse;
        return response.ContentLength;
    }

    public void Close()
    {
        isStop = true;
    }
}

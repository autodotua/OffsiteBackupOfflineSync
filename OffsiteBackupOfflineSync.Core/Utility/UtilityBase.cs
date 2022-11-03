using OffsiteBackupOfflineSync.Model;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace OffsiteBackupOfflineSync.Utility
{
    public class UtilityBase
    {
        /// <summary>
        /// 已经收到停止导出信号
        /// </summary>
        protected bool stopping = false;
        /// <summary>
        /// 停止导出（打包）
        /// </summary>
        public void Stop()
        {
            stopping = true;
        }
        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

        protected void InvokeMessageReceivedEvent(string message)
        {
            MessageReceived?.Invoke(this, new MessageEventArgs(message));
        }
        protected void InvokeProgressReceivedEvent(long current, long total)
        {
            ProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(current, total));
        }
        protected string GetMD5(string file)
        {
            using MD5 md5 = MD5.Create();
            using var stream = File.OpenRead(file);
            md5.ComputeHash(stream);
            return BitConverter.ToString(md5.Hash).Replace("-", "");
        }

        /// <summary>
        /// 复制并获取MD5
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        protected string CopyAndGetMd5(string from, string to)
        {
            int bufferSize = 1024 * 1024; //1MB的缓冲区
            using MD5 md5 = MD5.Create();
            using FileStream fileStream = new FileStream(to, FileMode.Create, FileAccess.Write, FileShare.None);
            using FileStream fs = new FileStream(from, FileMode.Open, FileAccess.Read);
            try
            {
                fileStream.SetLength(fs.Length);
                int bytesRead = -1;
                byte[] bytes = new byte[bufferSize];
                int offset = 0;
                while ((bytesRead = fs.Read(bytes, 0, bufferSize)) > 0)
                {
                    md5.TransformBlock(bytes, 0, bytesRead, null, 0);
                    fileStream.Write(bytes, 0, bytesRead);
                    offset += bytesRead;
                }
                md5.TransformFinalBlock(new byte[0], 0, 0);
                fs.Close();
                fs.Dispose();
                fileStream.Close();
                fileStream.Dispose();
                File.SetLastWriteTime(to, File.GetLastWriteTime(from));
                return BitConverter.ToString(md5.Hash).Replace("-", "");
            }
            catch (Exception ex)
            {
                fs.Close();
                fs.Dispose();
                fileStream.Close();
                fileStream.Dispose();
                if (File.Exists(to))
                {
                    try
                    {
                        File.Delete(to);
                    }
                    catch
                    {

                    }
                }
                throw;
            }
        }

        /// <summary>
        /// 文件是否在黑名单中
        /// </summary>
        /// <param name="name">文件名</param>
        /// <param name="path">文件路径</param>
        /// <param name="balckList">黑名单列表</param>
        /// <param name="blackRegexs">黑名单正则列表</param>
        /// <param name="blackListUseRegex">是否启用正则</param>
        /// <returns></returns>
        protected bool IsInBlackList(string name, string path, IList<string> balckList, IList<Regex> blackRegexs, bool blackListUseRegex)
        {
            for (int i = 0; i < balckList.Count; i++)
            {
                if (blackListUseRegex) //正则
                {
                    if (balckList[i].Contains('\\') || balckList[i].Contains('/')) //目录
                    {
                        if (blackRegexs[i].IsMatch(path))
                        {
                            return true;
                        }
                    }
                    else //文件
                    {
                        if (blackRegexs[i].IsMatch(name))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (balckList[i].Contains('\\') || balckList[i].Contains('/')) //目录
                    {
                        if (path.Contains(balckList[i]))
                        {
                            return true;
                        }
                    }
                    else //文件
                    {

                        if (name.Contains(balckList[i]))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

    }

}



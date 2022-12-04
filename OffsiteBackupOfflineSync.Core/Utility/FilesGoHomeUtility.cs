using FzLib;
using OffsiteBackupOfflineSync.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OffsiteBackupOfflineSync.Utility
{
    public class FilesGoHomeUtility : UtilityBase
    {
        List<GoHomeFile> filesGoHomeFiles = new List<GoHomeFile>();
        private Dictionary<long, object> length2Template = new Dictionary<long, object>();
        private Dictionary<DateTime, object> modifiedTime2Template = new Dictionary<DateTime, object>();
        private Dictionary<string, object> name2Template = new Dictionary<string, object>();
        public List<GoHomeFile> RightPositionFiles { get; private set; }
        public List<GoHomeFile> WrongPositionFiles { get; private set; }
        public void FindMatches(string templateDir, string sourceDir,
            bool compareName, bool compareModifiedTime, bool compareLength,
            string blackList, bool blackListUseRegex, int maxTimeTolerance)
        {
            if (!(compareName || compareModifiedTime || compareLength))
            {
                throw new ArgumentException("至少需要有一个比较类型");
            }
            //初始化变量
            stopping=false; 
            filesGoHomeFiles.Clear();
            InitializeBlackList(blackList, blackListUseRegex, out string[] blacks, out Regex[] blackRegexs);
            int sourceFileCount = 0;

            List<FileInfo> notMatchedFiles = new List<FileInfo>();
            List<FileInfo> matchedFiles = new List<FileInfo>();

            List<GoHomeFile> rightPositionFiles = new List<GoHomeFile>();
            List<GoHomeFile> wrongPositionFiles = new List<GoHomeFile>();
            List<GoHomeFile> tempFiles = new List<GoHomeFile>();


            //分析模板目录，创建由属性指向文件的字典
            CreateDictionaries(templateDir,maxTimeTolerance);

            //枚举源目录
            var sourceFiles = new DirectoryInfo(sourceDir)
            .EnumerateFiles("*", new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                AttributesToSkip = 0,
                RecurseSubdirectories = true,
            });

            //对每个源文件进行匹配分析
            foreach (var sourceFile in sourceFiles)
            {
                //黑名单检测
                if (IsInBlackList(sourceFile.Name, sourceFile.FullName, blacks, blackRegexs, blackListUseRegex))
                {
                    continue;
                }
                InvokeMessageReceivedEvent($"正在分析源文件：{sourceFile.FullName}");
                sourceFileCount++;
                matchedFiles.Clear();
                tempFiles.Clear();

                //与模板文件进行匹配
                bool notMatched = false;
                if (compareName) GetMatchedFiles(name2Template, sourceFile.Name);
                if (!notMatched && compareModifiedTime) GetMatchedFiles(modifiedTime2Template, TruncateToSecond(sourceFile.LastWriteTime));
                if (!notMatched && compareLength) GetMatchedFiles(length2Template, sourceFile.Length);

                if (notMatched)//无匹配，不需要继续操作
                {
                    notMatchedFiles.Add(sourceFile);
                    continue;
                }

                //对与该源文件匹配的模板文件（一般来说就一个）进行处理
                foreach (var templateFile in matchedFiles)
                {
                    //创建模板文件数据结构
                    var template = new FileBase()//模板文件
                    {
                        Path = Path.GetRelativePath(templateDir, templateFile.FullName),
                        Name = templateFile.Name,
                        Length = templateFile.Length,
                        LastWriteTime = templateFile.LastWriteTime,
                    };

                    //创建模型
                    var goHomeFile = new GoHomeFile()//源文件
                    {
                        Path = Path.GetRelativePath(sourceDir, sourceFile.FullName),
                        Name = sourceFile.Name,
                        Length = sourceFile.Length,
                        LastWriteTime = sourceFile.LastWriteTime,
                        MultipleMatchs = matchedFiles.Count > 1,
                        Template = template,
                    };

                    goHomeFile.RightPosition = template.Path == goHomeFile.Path;
                    tempFiles.Add(goHomeFile);
                }

                if (tempFiles.Count == 1)//如果只有一个匹配的文件
                {
                    if (tempFiles[0].RightPosition)//位置正确
                    {
                        rightPositionFiles.Add(tempFiles[0]);
                    }
                    else//位置错误
                    {
                        tempFiles[0].Checked = true;
                        wrongPositionFiles.Add(tempFiles[0]);
                    }
                }
                else//有多个匹配
                {
                    if (tempFiles.Any(p => p.RightPosition))
                    {
                        var tempWrongPositionFiles = tempFiles.Where(p => !p.RightPosition);
                        tempWrongPositionFiles.ForEach(p => p.Message = "包含一个位置正确的匹配");
                        wrongPositionFiles.AddRange(tempFiles.Where(p => !p.RightPosition));
                        rightPositionFiles.Add(tempFiles.First(p => p.RightPosition));
                    }
                    else
                    {
                        wrongPositionFiles.AddRange(tempFiles);
                    }
                }


                if (stopping)
                {
                    throw new OperationCanceledException();
                }

                //在指定字典中查找符合所给的属性值的文件
                void GetMatchedFiles<TK>(Dictionary<TK, object> dic, TK key)
                {
                    object files = null;
                        if (!dic.ContainsKey(key))
                        {
                            notMatched = true;
                            return;
                        }
                        files = dic[key];
                    //}

                    if (matchedFiles.Count == 0)//如果notMatched==false，但matchedFiles.Count==0，说明这是第一个匹配
                    {
                        if (files is FileInfo f)
                        {
                            matchedFiles.Add(f);
                        }
                        else if (files is List<FileInfo> list)
                        {
                            matchedFiles.AddRange(list);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else//如果不是第一个匹配，已经有规则匹配了
                    {
                        if (files is FileInfo f)
                        {
                            if (!matchedFiles.Contains(f))//单个文件，但是当前匹配不在之前匹配的文件中，说明匹配失败
                            {
                                matchedFiles.Clear();
                            }
                            else//否则，修改为本次匹配的单个文件
                            {
                                matchedFiles.Clear();
                                matchedFiles.Add(f);
                            }
                        }
                        else if (files is List<FileInfo> list)
                        {
                            var oldMatchedFiles = new List<FileInfo>(matchedFiles);
                            matchedFiles.Clear();
                            matchedFiles.AddRange(oldMatchedFiles.Intersect(list));
                        }
                        else
                        {
                            throw new Exception();
                        }
                        if (matchedFiles.Count == 0)//如果经过这一轮的匹配，没有任何文件完成匹配了，说明总的匹配失败
                        {
                            notMatched = true;
                        }
                    }
                }
            }


            RightPositionFiles = rightPositionFiles;
            WrongPositionFiles = wrongPositionFiles;
        }

        private void CreateDictionaries(string dir,int maxTimeTolerance)
        {
            name2Template.Clear();
            length2Template.Clear();
            modifiedTime2Template.Clear();
            var fileInfos = new DirectoryInfo(dir)
            .EnumerateFiles("*", new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                AttributesToSkip = 0,
                RecurseSubdirectories = true,
            });

            foreach (var file in fileInfos)
            {
                InvokeMessageReceivedEvent($"正在分析模板文件：{file.FullName}");
                SetOrAdd(name2Template, file.Name);
                SetOrAdd(length2Template, file.Length);
                for (int i=-maxTimeTolerance;i<=maxTimeTolerance;i++)
                {
                    SetOrAdd(modifiedTime2Template, TruncateToSecond(file.LastWriteTime).AddSeconds(i));
                }

                if(stopping)
                {
                    throw new OperationCanceledException();
                }
                void SetOrAdd<TK>(Dictionary<TK, object> dic, TK key)
                {
                    if (dic.ContainsKey(key))
                    {
                        if (dic[key] is List<FileInfo> list)
                        {
                            list.Add(file);
                        }
                        else if (dic[key] is FileInfo f)
                        {
                            dic[key] = new List<FileInfo>() { f, file };
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else
                    {
                        dic.Add(key, file);
                    }
                }

            }

        }


        public void CopyOrMove(IEnumerable<GoHomeFile> files,string sourceDir,string destDir, bool copy)
        {
            string copyMoveText = copy ? "复制" : "移动";
            long count = files.Sum(p => p.Length);
            long progress = 0;
            stopping = false;
            if(!copy)
            {
                files = files.Where(p => !p.RightPosition);
            }
            foreach (var file in files)
            {
                try
                {
                    InvokeMessageReceivedEvent($"正在{copyMoveText}：{file.Path}");
                    string destFile = Path.Combine(destDir, file.Template.Path);
                    string destFileDir = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destFileDir))
                    {
                        Directory.CreateDirectory(destFileDir);
                    }
                    if (copy)
                    {
                        File.Copy(Path.Combine(sourceDir, file.Path), destFile);
                        //Debug.WriteLine($"复制{Path.Combine(ViewModel.SourceDir, file.Path)}到{destFile}");
                    }
                    else
                    {
                        File.Move(Path.Combine(sourceDir, file.Path), destFile);
                        //Debug.WriteLine($"移动{Path.Combine(ViewModel.SourceDir, file.Path)}到{destFile}");
                    }
                    file.Complete = true;

                    if(stopping)
                    {
                        throw new OperationCanceledException();
                    }
                }
                catch (Exception ex)
                {
                    file.Message = ex.Message;
                }
                finally
                {
                    progress += file.Length;
                    InvokeProgressReceivedEvent(progress, count);
                }
            }
        }
    }
}

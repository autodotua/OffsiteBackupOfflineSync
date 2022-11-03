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
        private Dictionary<string, object> name2Template = new Dictionary<string, object>();
        private Dictionary<DateTime, object> modifiedTime2Template = new Dictionary<DateTime, object>();
        private Dictionary<long, object> length2Template = new Dictionary<long, object>();
        private void CreateDictionaries(string dir)
        {
            var fileInfos = new DirectoryInfo(dir)
            .EnumerateFiles("*", new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                AttributesToSkip = 0,
                RecurseSubdirectories = true,
            });

            foreach (var file in fileInfos)
            {
                name2Template[file.Name] = file;
                SetOrAdd(name2Template, file.Name);
                SetOrAdd(modifiedTime2Template, file.LastWriteTime);
                SetOrAdd(length2Template, file.Length);
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

        List<GoHomeFile> filesGoHomeFiles = new List<GoHomeFile>();
        public void FindMatches(string templateDir, string sourceDir,
            bool compareName, bool compareModifiedTime, bool compareLength,
            string blackList, bool blackListUseRegex, double maxTimeTolerance)
        {
            if (!(compareName || compareModifiedTime || compareLength))
            {
                throw new ArgumentException("至少需要有一个比较类型");
            }
            filesGoHomeFiles.Clear();
            string[] blacks = blackList.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            List<Regex> blackRegexs = blacks.Select(p => new Regex(p, RegexOptions.IgnoreCase)).ToList();
            CreateDictionaries(templateDir);
            var sourceFiles = new DirectoryInfo(sourceDir)
            .EnumerateFiles("*", new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                AttributesToSkip = 0,
                RecurseSubdirectories = true,
            });
            int sourceFileCount = 0;
            List<FileInfo> notMatchedFiles = new List<FileInfo>();
            List<FileInfo> matchedFiles = new List<FileInfo>();

            List<GoHomeFile> rightPositionFiles = new List<GoHomeFile>();
            List<GoHomeFile> wrongPositionFiles = new List<GoHomeFile>();
            List<GoHomeFile> tempFiles=new List<GoHomeFile>();
            foreach (var sourceFile in sourceFiles)
            {
                if (IsInBlackList(sourceFile.Name, sourceFile.FullName, blacks, blackRegexs, blackListUseRegex))
                {
                    continue;
                }
                sourceFileCount++;
                matchedFiles.Clear();
                tempFiles.Clear();

                bool notMatched = false;
                if (compareName) GetMatchedFiles(name2Template, sourceFile.Name);
                if (!notMatched && compareModifiedTime) GetMatchedFiles(modifiedTime2Template, sourceFile.LastWriteTime);
                if (!notMatched && compareLength) GetMatchedFiles(length2Template, sourceFile.Length);

                if (notMatched)//无匹配，不需要继续操作
                {
                    notMatchedFiles.Add(sourceFile);
                    continue;
                }

                foreach (var templateFile in matchedFiles)
                {
                    var template = new FileBase()//模板文件
                    {
                        Path = Path.GetRelativePath(templateDir, templateFile.FullName),
                        Name = templateFile.Name,
                        Length = templateFile.Length,
                        LastWriteTime = templateFile.LastWriteTime,
                    };
                    var goHomeFile = new GoHomeFile()//源文件
                    {
                        Path = Path.GetRelativePath(sourceDir, sourceFile.FullName),
                        Name = sourceFile.Name,
                        Length = sourceFile.Length,
                        LastWriteTime = sourceFile.LastWriteTime,
                        MultipleMatchs = matchedFiles.Count > 0,
                        Template = template,
                    };
                    goHomeFile.RightPosition = template.Path == goHomeFile.Path;
                    tempFiles.Add(goHomeFile);
                }
                if (tempFiles.Count == 1)
                {
                    if(tempFiles[0].RightPosition)
                    {
                        rightPositionFiles.Add(tempFiles[0]);
                    }
                    else
                    {
                        tempFiles[0].Checked = true;
                        wrongPositionFiles.Add(tempFiles[0]);
                    }
                }
                else
                {
                    这里需要判断是否存在位置正确的匹配，
                        如果有，那么所有临时文件不需要设置为Checked；
                        如果没有，那么设置第一个为Checked
                }

                void GetMatchedFiles<TK>(Dictionary<TK, object> dic, TK key)
                {
                    这里要加上对修改时间的特殊处理
                    if (!dic.ContainsKey(key))
                    {
                        notMatched = true;
                        return;
                    }

                    var files = dic[key];
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
                            //否则，不需要操作
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
        }
    }
}

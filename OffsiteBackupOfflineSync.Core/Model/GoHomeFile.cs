﻿namespace OffsiteBackupOfflineSync.Model
{
    public class GoHomeFile : FileBase
    {
        public GoHomeFile()
        {
        }
        public bool MultipleMatchs { get; set; }
        public bool RightPosition { get; set; }
        public FileBase Template { get; set; }
    }
}

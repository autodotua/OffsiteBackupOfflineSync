# 异地备份离线同步

在双方无法通过网络或实地同步的情况下，使用增量同步的方式，利用小容量设备完成异地和本地磁盘的数据同步

## 为谁开发？

- 如果你有重要数据，因此建立了异地备份
- 如果你的异地备份仅仅是单独放置、不连接网络的硬盘
- 如果你需要对异地备份硬盘需要进行定期同步，但又不想每次带着这些异地备份硬盘到本地来同步

## 步骤

1. **在异地**，建立异地硬盘的目录结构快照
2. **在本地**，将异地目录结构与本地进行对比，寻找差异部分，准备需要新增/更新/删除的文件
3. **在异地**，将更新文件应用到异地备份硬盘。

## 截图



## 日志

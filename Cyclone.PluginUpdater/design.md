# Cyclone.PluginUpdater 开发文档

> 适用范围：AutoCAD、Rhino 及其他 CAD 类二次开发插件  
> 下载源：Gitee 公开仓库  
> 客户端：独立 WPF 应用程序（Cyclone.PluginUpdater.exe）

---

## 一、架构设计

### 1.1 组件构成

系统由四个核心部分组成：

| 组件 | 位置 | 说明 |
|---|---|---|
| **Cyclone.PluginUpdater.exe** | 插件目录 | 独立 WPF 更新程序，不包含在 ZIP 包中 |
| **update.xml** | Gitee 仓库 Raw 文件 | 记录最新版本号、ZIP 下载地址、更新日志地址 |
| **changelog.html** | Gitee 仓库 Raw 文件 | 更新日志页面，固定地址，每次发版覆盖更新 |
| **plugin.zip** | Gitee Releases 附件 | 插件压缩包，不包含 Cyclone.PluginUpdater.exe |

```
插件目录/
├── MyPlugin.dll                   ← ZIP 中包含，会被覆盖
├── MyPlugin.gha                   ← ZIP 中包含，会被覆盖
├── Resources/                     ← ZIP 中包含，会被覆盖
└── Cyclone.PluginUpdater.exe      ← ZIP 中不包含，永不覆盖
```

```
Gitee 仓库/
├── update.xml                     ← Raw 文件，版本信息入口
├── changelog.html                 ← Raw 文件，固定地址，每次发版覆盖
└── Releases/
    └── v1.2.0/
        └── plugin.zip             ← 插件压缩包附件
```

### 1.2 整体流程概览

```
插件命令触发
    │
    ▼
启动 Cyclone.PluginUpdater.exe（传入参数）
    │
    ▼
先启动更新器，再退出宿主软件
    │
    ▼
请求 Gitee Raw → 解析 update.xml
    │
    ├─ 本地版本 >= 远程版本 → 提示"已是最新版本" → 退出
    │
    └─ 发现新版本
          │
          ▼
     界面展示新版本号
     [查看更新内容] [立即更新] [取消]
          │                │
          ▼                ▼
    下载 changelog.html   检测宿主进程是否运行
    写入程序目录               │
    调用系统默认浏览器打开  ┌────┴────┐
                        运行中     已退出
                          │           │
                       等待退出      继续
                          │           │
                          └────┬──────┘
                               ▼
                          下载 ZIP 至临时目录
                               │
                               ▼
                          备份旧目录
                               │
                               ▼
                          解压覆盖目标目录
                               │
                          ┌────┴────┐
                        成功         失败
                          │             │
                       删除备份      还原备份
                          │
                          ▼
                    提示更新完成
```

---

## 二、参数说明

插件启动 Cyclone.PluginUpdater.exe 时通过命令行参数传入所有必要信息，更新器本身不硬编码任何软件相关内容。

### 2.1 参数列表

| 参数名 | 必填 | 说明 | 示例值 |
|---|---|---|---|
| `--app-name` | 是 | 界面展示的插件名称 | `CAD 钢结构插件` |
| `--process` | 是 | 需要等待退出的宿主进程名（不含 .exe） | `acad` / `Rhino` |
| `--dir` | 是 | 插件安装目录，由插件运行时自行获取 | `C:\Users\...\插件目录` |
| `--xml-url` | 是 | Gitee 上 update.xml 的 Raw 文件地址 | `https://gitee.com/owner/repo/raw/master/update.xml` |
| `--current-version` | 是 | 当前版本号，从 Assembly 读取 | `1.0.0` |

### 2.2 插件端调用示例（C#）

```csharp
// 获取插件自身目录和版本号
string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3); // 取前三位：1.0.0
string updaterPath = Path.Combine(pluginDir, "Cyclone.PluginUpdater.exe");

string args = $"--app-name \"CAD 钢结构插件\" " +
              $"--process acad " +
              $"--dir \"{pluginDir}\" " +
              $"--xml-url \"https://gitee.com/owner/repo/raw/master/update.xml\" " +
              $"--current-version \"{currentVersion}\"";

// 注意：必须先启动更新器，再退出宿主软件
Process.Start(updaterPath, args);
Application.Quit();
```

### 2.3 不同软件的进程名参考

| 软件 | `--process` 参数值 |
|---|---|
| AutoCAD | `acad` |
| Rhino | `Rhino` |
| 中望 CAD | `zwcad` |
| 浩辰 CAD | `gcad` |

---

## 三、Gitee 文件规范

### 3.1 update.xml 结构

```xml
<item>
  <version>1.2.0</version>
  <url>https://gitee.com/owner/repo/releases/download/v1.2.0/plugin.zip</url>
  <changelog-url>https://gitee.com/owner/repo/raw/master/changelog.html</changelog-url>
</item>
```

| 字段 | 说明 |
|---|---|
| `version` | 最新版本号，格式为 `主版本.次版本.修订号` |
| `url` | plugin.zip 的直链下载地址 |
| `changelog-url` | changelog.html 的 Raw 文件地址，固定不变 |

### 3.2 changelog.html

固定存放于仓库根目录，地址永久不变。每次发版时直接覆盖该文件内容即可。更新器下载后写入程序目录（`change.html`），调用系统默认浏览器打开展示。

### 3.3 ZIP 包打包规范

ZIP 包内**只包含插件运行所需的文件**，严禁将 `Cyclone.PluginUpdater.exe` 打入包中。

```
plugin.zip
├── MyPlugin.dll
├── MyPlugin.gha          （如有）
└── Resources/            （如有）
```

### 3.4 发版流程

1. 更新 `changelog.html` 内容
2. 更新 `update.xml` 中的 `version` 和 `url` 字段
3. 在 Gitee 创建新 Release，Tag 名称格式为 `v1.2.0`
4. 上传 `plugin.zip` 作为 Release 附件
5. 将步骤 1、2 的文件提交到仓库 master 分支

---

## 四、异常处理机制

### 4.1 网络异常

| 场景 | 处理方式 |
|---|---|
| 无法请求 update.xml | 提示"无法连接服务器，请检查网络"，不影响旧版本使用 |
| changelog.html 下载失败 | "查看更新内容"按钮置灰，不阻塞更新流程 |
| ZIP 下载中断 | 删除不完整的临时文件，提示重试 |

### 4.2 文件操作异常

| 场景 | 处理方式 |
|---|---|
| 磁盘空间不足 | 解压前预估空间，不足时提前提示，不执行解压 |
| 解压过程失败 | 删除残留文件，将备份目录重命名还原，提示回滚成功 |
| 目录权限不足 | 提示以管理员身份运行 Cyclone.PluginUpdater.exe |
| 写入 change.html 失败（权限不足） | 弹窗提示检查插件目录读写权限 |

### 4.3 进程等待

```
轮询间隔：500ms
超时时间：60 秒
超时处理：提示"等待软件退出超时，请手动关闭后重试"
```

### 4.4 备份与回滚

```
备份目录命名：原目录名 + "_backup_" + 时间戳
示例：MyPlugin_backup_20260323_185030

成功：解压完成后删除备份目录
失败：删除解压残留 → 还原备份目录 → 提示用户
注意：创建新备份前若已存在旧备份，先将其删除
```

---

## 五、更新日志展示

用户点击"查看更新内容"后，更新器执行以下流程：

1. 从 `changelog-url` 下载 HTML 内容
2. 以 UTF-8 编码写入程序目录下的 `change.html`（固定文件名，每次覆盖）
3. 调用 `Process.Start` 以 `UseShellExecute = true` 打开该文件，由系统默认浏览器渲染展示

**选择此方案的原因：**
- WPF 内嵌 `WebBrowser` 控件基于 IE 内核，对现代 HTML/CSS 渲染效果差，中文排版尤为明显
- 系统默认浏览器（Edge / Chrome）体验更好，无需额外依赖
- 实现更简单，无需额外窗口

**注意事项：**
- 多个插件共用同一个更新器时，`change.html` 会互相覆盖，但内容始终是最后一次打开的插件的日志，不影响正常使用
- 若程序目录无写入权限，捕获 `UnauthorizedAccessException` 并提示用户检查目录读写权限

---

## 六、UI 设计规范

### 6.1 整体风格

遵循 Windows 10 原生控件风格，不使用任何第三方 UI 框架。

| 规范项 | 要求 |
|---|---|
| 圆角 | 全部禁用，所有控件使用直角（`CornerRadius="0"`） |
| 字体 | Microsoft YaHei UI，与系统一致 |
| 配色 | 遵循 Win10 系统色，主色调 `#0078D7`（Windows 蓝） |
| 控件风格 | 按钮、输入框、进度条均使用 Win10 原生扁平样式 |

### 6.2 主要控件样式要求

**按钮**
- 默认状态：背景 `#E1E1E1`，边框 `#ADADAD`，直角
- 主操作按钮（立即更新）：背景 `#0078D7`，文字白色
- Hover 状态：背景加深一级
- 禁用状态：透明度 60%

**进度条**
- 使用系统默认扁平样式，前景色 `#0078D7`，无圆角

**窗口**
- 标题栏使用系统原生标题栏，不自定义
- 窗口尺寸固定，不可缩放

---

## 七、其他说明

- Gitee 公开 API 有访问频率限制，更新器仅在用户主动触发时请求一次，不做后台轮询。
- `Cyclone.PluginUpdater.exe` 自身如需升级，需单独处理，建议保持逻辑足够简单以减少升级需求。
- 若后续需支持私有仓库，在参数中增加 `--token` 字段，请求时附加到 Header 即可，不影响现有逻辑。
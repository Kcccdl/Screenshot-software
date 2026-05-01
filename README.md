# 截图工具

Windows 桌面截图与标注软件，基于 C# + WPF (.NET 8) 开发。

界面采用 Win11 设计风格，明暗主题跟随系统自动切换。

## 核心功能

### 截图

- 全局热键（默认 PrintScreen，可自定义）一键触发截图
- 主窗口点击按钮触发截图
- 全屏截图 → 拖拽选择区域 → 自动进入编辑器
- 智能窗口检测：鼠标悬停自动高亮窗口边框，双击直接选中

### 8 种标注工具

| 工具 | 说明 |
|------|------|
| 矩形框 | 拖拽绘制矩形 |
| 椭圆/圆形 | 拖拽绘制椭圆 |
| 箭头 | 带三角形箭头，向量计算方向 |
| 直线 | 两点连线，圆头线帽 |
| 画笔涂鸦 | 自由线条，实时绘制 |
| 文字标注 | 支持字体、字号、粗体、斜体、下划线、删除线 |
| 马赛克/模糊 | 像素块平均化算法 |
| 序号标注 | 自增数字圆圈 |

### 编辑操作

- **选中**：左键点击已有标注
- **移动**：选中后拖拽
- **删除**：选中后按 Delete 键
- **文字二次编辑**：双击文字标注，在原有内容和样式基础上修改
- **撤销/重做**：Ctrl+Z / Ctrl+Y，基于 Command Pattern 双栈实现
- **颜色选择**：7 种预设颜色（红、绿、蓝、黄、橙、白、黑）
- **线条粗细**：滑块调节 1-10px
- **文字样式**：字体（微软雅黑、宋体、黑体、楷体、仿宋、Arial、Times New Roman、Consolas）、字号（12-48）、粗体、斜体、下划线、删除线

### 保存

- **复制到剪切板**（Ctrl+C）→ 可直接粘贴到微信、QQ、画图等应用
- **另存为**（Ctrl+S）→ 支持 PNG、JPEG、BMP 格式

## 技术架构

```
JIETU/
├── ScreenCapture.sln
└── src/ScreenCapture/
    ├── App.xaml/.cs              入口，截图流程编排
    ├── Windows/
    │   ├── MainWindow            主窗口（热键注册、截图入口）
    │   ├── SelectionOverlay      全屏选区覆盖层
    │   ├── AnnotationEditor      标注编辑器
    │   └── SettingsWindow        快捷键设置
    ├── Models/
    │   ├── Shapes/               8 种标注形状（IAnnotationShape 接口）
    │   ├── Commands/             撤销/重做命令（Command Pattern）
    │   └── AppSettings           设置持久化（JSON）
    ├── ViewModels/               MVVM ViewModel（CommunityToolkit.Mvvm）
    ├── Services/
    │   ├── ScreenCaptureService  GDI+ 屏幕截图
    │   ├── WindowDetector        Win32 窗口检测（WindowFromPoint）
    │   ├── HotkeyManager         全局热键（RegisterHotKey）
    │   ├── ClipboardService      剪切板操作
    │   └── SaveService           文件保存
    ├── Themes/                   Win11 明暗主题系统
    └── Utils/                    图标生成等工具
```

### 关键设计决策

| 决策 | 选择 | 原因 |
|------|------|------|
| 绘图画布 | Canvas（非 InkCanvas） | 完全控制子元素，支持独立命中测试和撤销删除 |
| 形状渲染 | WPF UIElement | 硬件加速渲染，支持属性绑定 |
| 元素追踪 | `_shapeVisuals` 字典 | 确保删除操作精确移除正确的实例 |
| 撤销/重做 | Command Pattern 双栈 | 标准设计模式，干净支持多级撤销 |
| 热键注册 | Win32 RegisterHotKey + HwndSource | 系统级全局热键，WPF 原生消息循环集成 |
| 主题系统 | 注册表检测 + 动态资源字典 | 实时跟随系统明暗模式切换 |
| 应用图标 | 运行时 GDI+ 生成 | 无需外部资源文件，首次生成后缓存复用 |

## 环境要求

- Windows 10/11
- .NET 8.0 运行时（独立部署可不需要）

## 构建与运行

### 开发环境

1. 安装 Visual Studio 2022 或 .NET 8 SDK
2. 打开 `ScreenCapture.sln`
3. 按 F5 运行

### 命令行构建

```bash
dotnet build src/ScreenCapture/ScreenCapture.csproj
```

### 发布单文件 EXE

```bash
dotnet publish src/ScreenCapture/ScreenCapture.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

产出：`publish/截图工具.exe`（单个文件，无需安装 .NET 运行时）

## 依赖

| 包 | 版本 | 用途 |
|----|------|------|
| CommunityToolkit.Mvvm | 8.2.2 | MVVM 基础设施（ObservableObject、RelayCommand） |
| System.Drawing.Common | 8.0.0 | GDI+ 屏幕截图（CopyFromScreen） |

## 使用方式

1. 启动程序后，按 **PrintScreen** 或点击「开始截图」按钮
2. 拖拽选择截图区域（双击可选中窗口）
3. 松开鼠标自动进入标注编辑器
4. 使用工具栏添加标注
5. 点击「复制到剪切板」或「另存为」保存

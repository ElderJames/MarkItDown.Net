# MarkItDown.Net

一个用于将各种文件格式转换为 Markdown 的 .NET 库，非常适合用于索引、文本分析和其他需要结构化文本的应用场景。本项目基于 [MarkItDownSharp](https://github.com/kelter-antunes/MarkItDownSharp) 的早期工作进行开发。

## 支持的格式

* PDF
* Word (.docx)
* Excel (.xlsx)
* 图片（EXIF 元数据提取和可选的 LLM 描述）
* 音频（仅 EXIF 元数据提取）
* HTML
* 文本格式（纯文本、.csv、.xml、.rss、.atom）
* Jupyter Notebooks (.ipynb)
* Bing 搜索结果页面 (SERP)
* ZIP 文件（递归遍历内容）
* PowerPoint (.pptx)
* Confluence（空间和单页面）

## 特性

- 现代化的 .NET 实现
- 增强的性能和可靠性
- 扩展的格式支持
- 改进的错误处理
- 全面的文档
- 可扩展的第三方服务集成

## 第三方服务支持

该库设计时考虑了扩展性，可以集成各种第三方服务以增强功能。

### 当前支持的服务

- **阿里云 OCR 服务**
  - 文档文字识别
  - 表格结构识别
  - 手写文字识别
  - 更多基于阿里云的 OCR 能力

### 添加新服务

本库提供了灵活的插件架构，用于添加新的服务集成。关于如何实现自定义服务提供者的文档将很快提供。

## 快速开始

[即将推出]

## 贡献

欢迎提交贡献！请随时提交 Pull Request。

## 致谢

本项目基于 kelter-antunes 的 [MarkItDownSharp](https://github.com/kelter-antunes/MarkItDownSharp) 早期工作。我们感谢他们的初始实现，为这个增强版本提供了坚实的基础。

## 许可证

[许可证信息即将推出]

## 注意

音频转换器的语音识别功能计划在未来实现。我们特别欢迎这方面的贡献。 
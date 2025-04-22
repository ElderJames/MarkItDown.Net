# MarkItDown.Net

A .NET library for converting various file formats to Markdown, making it ideal for indexing, text analysis, and other applications that benefit from structured text. This project builds upon the early work of [MarkItDownSharp](https://github.com/kelter-antunes/MarkItDownSharp).

## Supported Formats

* PDF
* Word (.docx)
* Excel (.xlsx)
* Images (EXIF metadata extraction and optional LLM-based description)
* Audio (EXIF metadata extraction only)
* HTML
* Text-based formats (plain text, .csv, .xml, .rss, .atom)
* Jupyter Notebooks (.ipynb)
* Bing Search Result Pages (SERP)
* ZIP files (recursively iterates over contents)
* PowerPoint (.pptx)
* Confluence (spaces and single pages)

## Features

- Modern .NET implementation
- Enhanced performance and reliability
- Expanded format support
- Improved error handling
- Comprehensive documentation
- Extensible third-party service integration

## Third-Party Services Support

The library is designed with extensibility in mind, allowing integration with various third-party services for enhanced functionality.

### Currently Supported Services

- **Aliyun OCR Service**
  - Document text recognition
  - Table structure recognition
  - Handwriting recognition
  - More OCR capabilities based on Aliyun's offerings

### Adding New Services

The library provides a flexible plugin architecture for adding new service integrations. Documentation for implementing custom service providers will be available soon.

## Getting Started

[Coming soon]

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

This project is based on the early work of [MarkItDownSharp](https://github.com/kelter-antunes/MarkItDownSharp) by kelter-antunes. We are grateful for their initial implementation which provided a solid foundation for this enhanced version.

## License

[License information coming soon]

## Note

Speech Recognition for audio converter is planned for future implementation. Contributions in this area are especially welcome.

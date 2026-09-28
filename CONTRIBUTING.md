# Contributing to Adult ColoringBook

Thank you for your interest in contributing to **Adult ColoringBook**! We welcome and appreciate all community contributions, from reporting bugs and suggesting new features to crafting new coloring artwork designs, brushes, or UI improvements.

## How Can I Contribute?

### Reporting Bugs
If you find a bug:
1. Check the [Issues tab](https://github.com/uponatime2019/NewColoringbook/issues) to see if it has already been reported.
2. If not, open a new issue with a descriptive title and clear steps to reproduce the issue. Include system details (Windows 10/11 version, display scaling, etc.).

### Suggesting Enhancements & New Artwork
- Have an idea for a new coloring catalog category (e.g. Architecture, Ocean Life, Steampunk)?
- Have ideas for relaxing ambient tracks or textured fill styles?
- Feel free to open a feature request issue or submit vector artwork paths to the `DesignCatalog`.

### Code Contributions & Pull Requests
1. **Fork** the repository and create your feature branch:
   ```bash
   git checkout -b feature/amazing-feature
   ```
2. Make your improvements, keeping code readable, modern C#, and conforming to WinUI 3 best practices.
3. Test your build locally:
   ```powershell
   dotnet build "NewColoringbook.csproj" -p:Platform=x64
   ```
4. Verify the self-contained Release publish runs properly:
   ```powershell
   dotnet publish "NewColoringbook.csproj" -c Release -p:Platform=x64 -o publish_test
   ```
5. Commit your changes with clear, concise messages.
6. Push to your branch and submit a **Pull Request**.

## Code Style & Guidelines
- Target .NET 8 and Windows App SDK 2.4+.
- Avoid external proprietary store dependencies.
- Ensure unpackaged compatibility using `StoragePaths` or local application directories.

We're excited to build a peaceful, creative coloring experience together!

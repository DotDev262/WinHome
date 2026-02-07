# Contributing to WinHome

First off, thank you for considering contributing to WinHome! It's people like you that make WinHome such a great tool.

## Where do I go from here?

If you've noticed a bug or have a feature request, [make one](https://github.com/DotDev262/WinHome/issues/new/choose)! It's generally best if you get confirmation of your bug or approval for your feature request this way before starting to code.

### Fork & create a branch

If this is something you think you can fix, then [fork WinHome](https://github.com/DotDev262/WinHome/fork) and create a branch with a descriptive name.

A good branch name would be (where issue #38 is the ticket you're working on):

```sh
git checkout -b 38-add-awesome-new-feature
```

### Get the code

```sh
git clone https://github.com/your-username/WinHome.git
cd WinHome
```

### Build the project

To build the project, run the following command from the root of the repository:

```sh
dotnet build WinHome.sln
```

### Run the tests

To run the tests, run the following command from the root of the repository:

```sh
dotnet test WinHome.sln
```

## 🐧 Linux & 🍎 macOS Development

You can develop and run unit tests for WinHome on Linux and macOS using the .NET 10 SDK. Since the engine is Windows-specific, we use mocks to ensure high test coverage on non-Windows platforms.

For a detailed guide on how to contribute from a non-Windows machine, see the **[Cross-Platform Development Guide](./docs/cross-platform-dev.md)**.

### Make your changes

Now, go make your changes!

### Commit your changes

Make sure your commit messages are in the proper format.

```sh
git commit -m "Fix: Brief description of the change"
```

### Push your changes

```sh
git push origin 38-add-awesome-new-feature
```

### Create a pull request

Go to the GitHub repository and create a pull request.

## A note on pull requests

- Fill the pull request template out completely.
- A link to the GitHub issue in the pull request is required.
- Make sure your code is formatted correctly.
- Make sure your code is tested.

Thank you for your contribution!

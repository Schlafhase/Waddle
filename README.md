<!-- GENERATED FILE. DO NOT EDIT -->

# Waddle: Penguins with packets 📦🐧

<img width="1282" height="283" alt="image" src="https://github.com/user-attachments/assets/3dce8a42-17ba-4ac5-aee7-70dbe36a5b39" />

Waddle is a tool that automates deploying your project to servers like nest. It
can also be used to automate other kinds of client/server interaction. Think of
it as little penguins waddling back and forth between your client and your
server (or nest).

https://github.com/user-attachments/assets/4116d550-c0f4-4232-baba-508ac92ed45f

The video shows me using Waddle to deploy a Nextjs project. I added the text
"Deployed using Waddle" which can be seen at the end of the video.

# Table of Contents

- [Quick Start](#quick-start)
  - [How to clean up](#how-to-clean-up)
- [Installation](#installation)
  - [Using dotnet tools](#using-dotnet-tools)
  - [Manual installation](#manual-installation)
- [Usage](#usage)
  - [Creating workflows](#creating-workflows)
    - [Example](#example)
  - [Running Workflows](#running-workflows)
- [Full config specification](#full-config-specification)
- [For developers](#for-developers)
  - [Test Server](#test-server)
  - [Project overview](#project-overview)
- [How it works](#how-it-works)
- [Planned features](#planned-features)

> [!NOTE]
>
> **Use of AI:** I used Claude to help me with the Dockerfile for the test
> server.

## Quick Start

**Requirements:** Docker, .NET SDK

If you just want to try the project temporarily follow these instructions in any
directory:

```sh
# Clone the whole repository because it contains a test workflow
git clone https://github.com/Schlafhase/Waddle
cd Waddle
dotnet tool install -g Waddle.Cli
# Create and start docker container
./TestServer/start.sh
```

Now you can start playing around:

> [!NOTE]
>
> The credentials for the docker container are
>
> - **Host:** localhost
> - **Username:** root
> - **Port:** 2222
> - **Password:** Docker!

```sh
waddle init
# Run test workflow
waddle Waddle.Cli/test
```

### How to clean up

```sh
# Remove the docker container
docker rm -f waddle-test-server
# Uninstall the tool
dotnet uninstall -g Waddle.Cli
# Remove directory
cd ..
rm -rf ./Waddle/
```

## Installation

### Using dotnet tools

If you have the .NET SDK installed, you can install Waddle with this simple
command:

```sh
dotnet tool install -g Waddle.Cli
```

#### On NixOS

```nix
environment.systemPackages = [
    # ...
    (buildDotnetGlobalTool {
      pname = "waddle";
      nugetName = "Waddle.Cli";
      version = "0.4.1";
      nugetHash = "sha256-tACHDgvmmXZNwDn7qgcv+iCle1X154HrekdV8KQ7jiQ=";
      dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
    })
];
```

Or with home manager:

```nix
home.packages = [
    # ...
    (buildDotnetGlobalTool {
      pname = "waddle";
      nugetName = "Waddle.Cli";
      version = "0.4.1";
      nugetHash = "sha256-tACHDgvmmXZNwDn7qgcv+iCle1X154HrekdV8KQ7jiQ=";
      dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
    })
];
```

### Manual installation

> [!NOTE]
>
> The binary for Windows hasn't been tested yet

<!--TODO: test windows version 😔-->

Grab the binary from the
[latest release](https://github.com/Schlafhase/Waddle/releases/) (`waddle` for
Linux and `waddle.exe` for Windows). Move the binary to a directory in your
PATH. You should be ready to go now.

## Usage

Go to your projects directory and run `waddle init`. After answering all
questions, you should see `waddle.yaml`. You can check its content to confirm
your configuration.

### Creating workflows

To create a workflow, create a yaml file with the name of your workflow. Each
workflow is a yaml sequence of actions (I call them penguins). Every penguin
needs a name and some parameters depending on the type. Here is the list of all
available penguins:

| Penguin                                                                                                                 | Description                                                                                                                                                                        | Parameters                                       |
| ----------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| [RunServerCommand](https://github.com/Schlafhase/Waddle/blob/master/Penguins/ServerPenguins/RunServerCommandPenguin.cs) | Runs a command on the on the server via SSH                                                                                                                                        | `serverCmd` (string)                             |
| [SendFile](https://github.com/Schlafhase/Waddle/blob/master/Penguins/ServerPenguins/SendFilePenguin.cs)                 | Uploads a single file to a destination (file!) on the server                                                                                                                       | `sendFile` (string), `destination` (string)      |
| [ReceiveFile](https://github.com/Schlafhase/Waddle/blob/master/Penguins/ServerPenguins/ReceiveFilePenguin.cs)           | Downloads a single file from the server to a destination (file!) on the client                                                                                                     | `receiveFile` (string), `destination` (string)   |
| [ReceiveFolder](https://github.com/Schlafhase/Waddle/blob/master/Penguins/ServerPenguins/ReceiveFolderPenguin.cs)       | Downloads a folder from the server to a destination (directory!) on the client                                                                                                     | `receiveFolder` (string), `destination` (string) |
| [SendFolder](https://github.com/Schlafhase/Waddle/blob/master/Penguins/ServerPenguins/SendFolderPenguin.cs)             | Uploads a folder to a destination (directory!) on the server                                                                                                                       | `sendFolder` (string), `destination` (string)    |
| [RunCommand](https://github.com/Schlafhase/Waddle/blob/master/Penguins/ClientPenguins/RunCommandPenguin.cs)             | Runs a command on the client using the value of `shell` as shell or `sh` (Linux) or `cmd.exe` (Windows). `shell` must be something like `["sh", "-c"]` (in yaml syntax of course). | `cmd` (string), `shell` (List\<string\>)         |

> [!WARNING]
>
> Defining parameters matching multiple penguins can lead to unexpected
> behaviour

### Additional Parameters

In addition to the required parameters, each penguin can define these optional
parameters:

- `ignoreError` (bool): If true, the workflow will continue if this penguin
  fails
- `timeoutMs` (int): Sets the timeout in milliseconds

#### Example

Here is the workflow I used in the demo video:

```yaml
# deploy.yaml
- name: Build # RunCommand penguin
  cmd: npm run build
  shell: # Use custom shell (since sh is the default on Linux, this is redundant and just for demonstration)
    - sh
    - -c

- name: Remove unnecessary files from build # RunCommand penguin
  cmd: rm -rf ./.next/standalone/node_modules
  ignoreError: true

- name: Stop systemd service # RunServerCommand penguin
  serverCmd: systemctl stop --now devqed

- name: Copy files to Server # SendFolder penguin
  sendFolder: ./.next/standalone/
  destination: /home/Linus/Projects/qed/QED-Online/.next/standalone/

- name: Start systemd service # RunServerCommand penguin
  serverCmd: systemctl start --now devqed
  ignoreError: true
```

### Running Workflows

**The quick answer:** `waddle {workflow name}`

**The details:**

You can run any workflow using `waddle {workflow name}`. For example, use
`waddle build` to run the workflow specified in `build.yaml` or `build.yml` (You
may also use `.w.yaml` and `.w.yml` to avoid conflicts with other tools). You
may specify the file ending explicitly but you don't need to.

You can specify a default workflow in the config file `waddle.yaml` or using
`waddle init`. Use the `waddle` command without any arguments to run the default
workflow.

## Full config specification

You can edit these in `waddle.yaml`

> Extracted from [WaddleConfig.cs](https://github.com/Schlafhase/Waddle/blob/master/Waddle.Config/WaddleConfig.cs)

```cs
public WaddleServerConfig? Server;

public string? ClientOutputFileName;

public string? LogFileName;
public LogLevel LogLevel; // Trace | Debug | Information | Warning | Error | Critical

public string FinishedIcon;
public string WaitingIcon;
public string ErrorIcon;
public string IgnoredIcon;
public string NotActiveIcon;

public required string DefaultWorkflow;
public List<string>? DefaultShell; // e.g. ["sh", "-c"]

public bool VerboseErrors;
```

### Server config

> Extracted from [WaddleConfig.cs](https://github.com/Schlafhase/Waddle/blob/master/Waddle.Config/WaddleConfig.cs)

```cs
public required string Host;
public int Port;
public required string Username;

public bool UsePassword;
public string? Keyfile;
public bool UseSshAgent;

public string? ServerOutputFileName;
```

## For developers

This project needs the .NET SDK to be installed. Run these commands to set up
the codebase:

```sh
git clone https://github.com/Schlafhase/Waddle
cd Waddle
dotnet build
```

### Test Server

This repository comes with a Dockerfile that can set up a server that runs SSH
on it. Follow these instructions to set it up:

```sh
# cp ~/.ssh/id_ed25519.pub ./TestServer/id_ed25519.pub # Uncomment this if you want private key authentication
docker build -t ssh-container ./TestServer
docker create -p 2222:22 -t --name waddle-test-server ssh-container
docker start waddle-test-server
```

Then use these values in your `waddle.yaml`:

```yaml
# ...
Host: localhost
Username: root
Port: 2222
# Keyfile: ~/.ssh/id_ed25519 # If you copied your public key before building the docker image
# UsePassword: true # If you didn't copy the public key
# ...
```

> [!NOTE]
>
> If you use password authentication, the password is **Docker!**

### Project overview

- **Penguins:** This project contains the implementations for every penguin
- **TestServer:** Contains a Dockerfile to build a quick server for testing
- **Waddle.Cli:** The Command-Line-Interface. Can be run with `dotnet run`
- **Waddle.Config:** Everything yaml-related

## How it works

The workflow parsed from the yaml parser gets converted to penguins using
pattern-matching. There are patterns to check for required parameters of
specific penguins. Every penguin imlements `Penguins.IPenguin` which ensures
they have an async `Execute` method. That makes it easy to execute each penguin.

> [!NOTE]
>
> If you implement your own penguins, you'll likely want to inherit from
> `Penguins.PenguinBase`.

Waddle uses SSH and SFTP (SSH File Transfer Protocol) under the hood. Penguins
that need client/server interaction will use these technologies.

## Planned features

- Unit tests
- Check fingerprint
- Choose shell program
- Allow nested workflows
- Add SendCompressedFolderPenguin
- Add ReceiveCompressedFolderPenguin
- Add client-only workflows _(Implemented but not thoroughly tested)_
- Workflow variables
- `waddle new` command to add a workflow
- Run waddle workflows from the directory of the workflow to prevent unexpected
  behaviour

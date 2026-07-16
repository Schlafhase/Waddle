# Waddle: Penguins with packets 📦🐧

Waddle is a tool that automates deploying your project to servers like nest. It
can also be used to automate other kinds of client/server interaction. Think of
it as little penguins waddling back and forth between your client and your
server (or nest).

![Demo](./media/Waddle_demo.mp4)

The video shows me using Waddle to deploy a Nextjs project. I added the text
"Deployed using Waddle" which can be seen at the end of the video.

## Installation

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

| Penguin          | Description                                                                    | Parameters                                       |
| ---------------- | ------------------------------------------------------------------------------ | ------------------------------------------------ |
| RunCommand       | Runs a command on the client using `sh` (Linux) or `cmd.exe` (Windows)         | `cmd` (string)                                   |
| RunServerCommand | Runs a command on the server using SSH.                                        | `serverCmd` (string)                             |
| SendFolder       | Uploads a folder to a destination (directory!) on the server                   | `sendFolder` (string), `destination` (string)    |
| ReceiveFolder    | Downloads a folder from the server to a destination (directory!) on the client | `receiveFolder` (string), `destination` (string) |
| SendFile         | Uploads a single file to a destination (file!) on the server                   | `sendFile` (string) `destination`                |
| ReceiveFile      | Downloads a single file from the server to a destination (file!) on the client | `receiveFile` (string) `destination` (string)    |

> [!NOTE]
>
> Defining parameters matching multiple penguins can lead to unexpected
> behaviour

#### Example

Here is the workflow I used in the demo video:

```yaml
# deploy.yaml
- name: Build # RunCommand penguin
  cmd: npm run build

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

```cs
const string Version = "0.3";

required string Host;
int Port;
required string Username;

bool UsePassword;
string? Keyfile;
bool UseSshAgent;

string? ServerOutputFileName;
string? ClientOutputFileName;

string? LogFileName;
LogLevel LogLevel;

string FinishedIcon;
string WaitingIcon;
string ErrorIcon;
string IgnoredIcon;
string NotActiveIcon;

required string DefaultWorkflow;

bool VerboseErrors;
```

# Waddle Devlog 3 :penguin-noted:

## What changed? :pr:

- Added a timeout option for _penguins_ (my name for workflow tasks)
- Added SendFolderPenguin and ReceiveFolderPenguin which can send and receive
  folders from the server
- Added Logging
- Added SendFilePenguin and ReceiveFilePenguin which can send and receive files
- Improved the CLI
- Got really stressed out trying to become #1 on the globaly hackatime
  leaderboard 😅
- Managed to become #1 on the global Hackatime leaderboard in last 24 hours
  :yayayayayay-67-nb:
- Added publish waddle workflow so I can build the binaries using waddle
  :hehehe:

## What's planned? :plane-flapping:

- Completely refactor the workflow runner so that nested workflows can be
  allowed :yay:
- SendCompressedFolderPenguin :yayayayayayay:
- ReceiveCompressedFolderPenguin :nayayayayay:

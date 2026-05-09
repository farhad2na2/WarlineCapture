# Codex Local Relay

Poll a private Slack channel and run local Codex tasks on this Mac.

This is for the phone workflow:

```text
phone Slack message -> this relay on the Mac -> codex exec -> WarlineCapture-SlackAgent -> Slack reply
```

## Slack App Setup

Create a Slack app for your workspace:

1. Go to <https://api.slack.com/apps>.
2. Create an app from scratch.
3. Add a bot user.
4. Under **OAuth & Permissions**, add these bot token scopes:
   - `chat:write`
   - `channels:history` if you use a public channel
   - `groups:history` if you use a private channel
5. Install the app to the workspace.
6. Copy the bot token. It starts with `xoxb-`.
7. Invite the bot to your private channel:

```text
/invite @your-bot-name
```

Get the channel ID from Slack by opening the channel details. It usually starts with
`C` for public channels or `G` for private channels.

## Local Config

Copy the example config:

```bash
cp Tools/CodexLocalRelay/config.example.env Tools/CodexLocalRelay/.env.local
```

Edit `Tools/CodexLocalRelay/.env.local` and fill in:

```text
SLACK_BOT_TOKEN=xoxb-...
SLACK_CHANNEL_ID=G...
```

The default worktree is:

```text
/Users/farhad/Projects/WarlineCapture-SlackAgent
```

## Run

```bash
python3 Tools/CodexLocalRelay/codex_local_relay.py
```

The relay posts an online message and then watches for messages that start with:

```text
codex local
```

## Jenkins Failure Queue

The relay can also watch a local task directory for Jenkins failure tasks. Set
`CODEX_TASK_DIR` in `.env.local` to a folder that Jenkins can write to and this
Mac can read:

```text
CODEX_TASK_DIR=/Users/farhad/Projects/Jenkins_Builds/WarlineCapture/CodexTasks
```

`Jenkinsfile.groovy` runs `Tools/CI/QueueCodexJenkinsFailure.ps1` in the
pipeline `post { failure { ... } }` block. The script copies `build.log`,
`TestResults/*.xml`, and `TestResults/*.log` into a failure bundle, then writes a
JSON task into `CODEX_TASK_DIR`. On the next relay poll, Codex starts a local
investigation task and posts the result back to Slack.

For the handoff to work, Jenkins' `CODEX_TASK_DIR` path and the relay's
`CODEX_TASK_DIR` must refer to the same shared folder. If Jenkins writes from
Windows over SMB, configure the Jenkins environment variable with the Windows
share path and configure the relay with the matching local Mac path.

## Slack Commands

Check status:

```text
codex local status
```

Start a local Codex task:

```text
codex local start task: inspect the WarlineCapture tactical HUD code and summarize what is unfinished. Do not edit files.
```

Cancel the active local Codex task:

```text
codex local cancel
```

Show help:

```text
codex local help
```

## Safety Defaults

- The relay only reacts to messages beginning with `codex local`.
- It runs one Codex task at a time.
- It targets `WarlineCapture-SlackAgent`, not the main active checkout.
- It uses `codex exec` with `workspace-write` sandboxing.
- It writes logs to `/private/tmp/codex-local-relay`.

## Local Voice Page

For a natural voice workflow on the laptop, run:

```bash
python3 Tools/CodexLocalRelay/codex_voice_server.py
```

Then open:

```text
http://127.0.0.1:8765
```

Click **Start Listening** once, speak naturally, and the page will transcribe
the recorded voice with local Whisper, send the transcribed request to local
Codex, and play one locally generated voice reply.

Install the local voice dependencies:

```bash
brew install whisper-cpp ffmpeg espeak-ng
```

Notes:

- Audio recording works best in Chrome on `localhost`.
- The browser does not perform speech recognition or text-to-speech. It only
  records microphone audio and plays the returned audio.
- The default multilingual Whisper model is stored outside Git at
  `.local/whisper-models/ggml-small.bin`.
- Persian speech output uses local `espeak-ng`, which is functional but less
  natural than ChatGPT Voice.
- The voice page does not require the `codex local` trigger phrase.
- It targets the same `CODEX_WORKTREE` as the Slack relay.
- Phone browsers may require HTTPS before microphone recording works.

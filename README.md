# Gloss

## Overview

Gloss is a self-hosted, always-on code review assistant that watches your Git projects for new merge requests, reviews them using an LLM, and presents the results in a local web UI for you to inspect, edit, and publish.

It runs entirely as a Docker container on your machine. You never modify your Git server. Gloss only ever initiates outbound connections — to your Git platform to fetch MRs and post comments, and to your LLM provider to generate reviews. Nothing reaches into your environment.

The review workflow is staged: the LLM's comments land in a draft state first. You open the UI whenever you feel like it, see the diff side-by-side with inline annotations, edit or discard what doesn't fit, add your own, and when you're satisfied you publish — which posts everything to the actual MR in one shot.

Every action you take in that editing stage is recorded as an event. Over time, the LLM reads those events and updates a persistent projection of your review style — what you care about, what you ignore, how you phrase things. That projection is injected into every subsequent review, so Gloss gradually learns to think like you.

---

## How It Works

### 1. Polling

Gloss runs a background poller that periodically calls your Git platform's REST API to list open MRs across configured projects. Each MR is identified by its platform-specific ID and stored as an entity in the database. If an MR has already been seen and hasn't been updated since the last check, it is skipped.

### 2. Repo Cache

When an MR is encountered for the first time from a given repository, Gloss clones that repository into a managed volume (`/repos/<project-slug>/`). On every subsequent MR from the same repo, it runs a `git fetch` to bring the clone up to date. This means Gloss always has a full local copy of the codebase to work with — not just the diff — which gives the LLM the context it needs to understand what changed and why.

### 3. Review

Once the repo is ready, Gloss builds the review context:

- The raw diff for the MR branch
- The full content of every changed file (from the local clone)
- Any directly referenced files detected from imports and namespaces
- The constitution — your team's explicit review guidelines and standards (always present, never modified by learning)
- The reviewer projection — the LLM's accumulated model of your personal style, built on top of the constitution

This context is sent to the configured LLM provider. The response is a structured list of inline comments — each tied to a file and line — plus an optional reasoning trace explaining why each comment was made.

### 4. Staging

All comments land in a draft state in the database. Nothing is posted to your Git platform yet. The MR moves to a **Ready** state, waiting for you.

### 5. The UI

You open `localhost` in a browser whenever you want. The UI shows all MRs grouped by state:

| State | Meaning |
|---|---|
| Pending | Review in progress or queued |
| Ready | Reviewed, not yet opened by you |
| Seen | You opened it, made no changes |
| Staged | You have edited or added comments |
| Published | Posted to the MR on your Git platform |

Opening an MR shows the full diff with inline draft comments anchored to their lines. Each comment has a collapsible reasoning panel. You can edit the text, delete the comment, or add new ones. When you're done, you hit **Publish** and all staged comments are posted to the MR via the REST API in one request.

### 6. Learning

Every edit is recorded as an event:

- `CommentAccepted` — you published it unchanged
- `CommentEdited` — you changed the text (before and after stored)
- `CommentDeleted` — you removed it
- `CommentAdded` — you wrote one the LLM didn't

After each publish, Gloss triggers a projection update: the LLM reads the current projection plus all new events since the last update — including the full text of every edit, deletion, and user-written comment — and produces a revised projection. The projection captures both what you flag and how you say it: your phrasing, tone, length, and level of directness. Over time it becomes a precise, distilled model of how you review code, written in your voice.

---

## Functional Requirements

### Poller
- Periodically query the configured Git platform for open MRs across all configured projects
- Detect new MRs and MRs updated since the last poll (by updated timestamp or commit SHA)
- Skip MRs that are already in a terminal state (Published, or explicitly dismissed)
- Survive transient failures — network down, VPN disconnected, API rate limits — by retrying with backoff without losing state
- Poll schedule is a Quartz cron expression defined per repository in the database, falling back to `DEFAULT_POLL_CRON` when not set

### Repo Manager
- On first encounter of a repository, clone it in full into the managed volume
- On subsequent MRs from the same repository, run a fetch to bring it up to date before reviewing
- Store one clone per repository, identified by a stable slug derived from the project path
- Never block the poller — cloning and fetching happen as part of the review job, not inline

### Reviewer
- Build a review context per MR: raw diff, full content of changed files, directly referenced files where detectable, and the current reviewer projection
- Send the context to the configured LLM provider and parse the response into a structured list of comments (file, line, body, optional reasoning)
- Store all comments as drafts — nothing is posted to the Git platform at this stage
- If the LLM call fails, mark the job for retry; partial results are discarded
- Attach the reasoning trace to each comment if the provider returns one

### Draft Store
- Persist all draft comments with their MR, file, line, body, reasoning, and current state (Draft, Edited, Deleted, UserAdded)
- Track MR-level state transitions: Pending → Ready → Seen → Staged → Published
- Support editing a comment body, deleting a comment, and adding new comments not originating from the LLM
- Never mutate the original LLM output — edits are stored as deltas alongside the original

### Web UI
- Show all MRs grouped by state on a dashboard
- MR detail view: full diff rendered with syntax highlighting, inline draft comments anchored to their lines
- Each comment shows the editable body and a collapsible reasoning panel
- Allow editing, deleting, and adding comments without leaving the diff view
- When editing, deleting, or adding a comment, show an optional free-text reason field ("Why are you making this change?") — stored in the event payload to give the projection engine precise, scoped context rather than forcing it to generalise from the action alone
- Publish button posts all non-deleted comments to the Git platform and transitions the MR to Published
- Repository settings panel: view all tracked repositories and edit their `PollCron` override directly from the UI
- Constitution panel: add, edit, delete, and reorder constitution documents; button to trigger a one-time projection seed from current documents
- Config page: view and edit all provider configuration — Git platform, LLM provider, API keys, default poll schedule — stored encrypted in the database; changes take effect immediately without restarting the container
- No login, no auth — localhost only

### Event Log
- Record every user action as an immutable event appended to the event log
- Minimum event types: `CommentAccepted`, `CommentEdited` (with before/after), `CommentDeleted`, `CommentAdded`, `MRPublished`
- Events are never updated or deleted — they are the source of truth for the learning loop
- Each event carries a timestamp, the MR it belongs to, and the comment it concerns

### Constitution
- Store a collection of named documents (e.g. code review guidelines, architecture standards, coding style) in the database
- All constitution documents are injected into every review context as a permanent, authoritative layer — they are never modified by the learning loop
- Constitution documents can optionally be used to seed the initial reviewer projection, giving it an informed baseline before any events have been recorded
- Manage documents via the UI: add, edit, delete, reorder
- Documents are ordered — earlier documents are treated as higher priority in case of conflict

### Projection Engine
- After each MR is published, trigger a projection update
- The LLM reads the current projection document plus all events since the last update — including full text payloads — and returns a revised projection
- The projection covers two dimensions: **behavioural** (what the reviewer flags, ignores, or adds) and **stylistic** (how they phrase comments — tone, length, vocabulary, level of directness). Both are derived from the actual comment text in event payloads, not just event types.
- When a reason is present on an event, the projection engine must treat it as a scoping constraint — learning a specific, bounded rule rather than a broad generalisation. A deletion with a reason of "capped at 10 items by business rule" should produce a rule like "skip pagination comments when the dataset is provably bounded" not "pagination is unimportant".
- Store the projection with a version and timestamp; previous versions are retained for debugging
- Inject the current projection into the review context for every new MR

### Configuration
- All configuration via environment variables and/or a single config file mounted into the container
- Required: Git platform type, base URL, personal access token, list of projects to watch
- Required: LLM provider type, API key, model name
- Optional: poll interval, repo volume path, reasoning enabled/disabled

---

## Architecture

### Containers

Gloss is composed of two containers defined in a single `docker-compose.yml`:

| Container | Role |
|---|---|
| `gloss` | The main application: background worker + ASP.NET Core web server |
| `postgres` | Persistent storage for MR state, draft comments, event log, and projections |

```
┌─────────────────────────────────────────┐
│               gloss container           │
│                                         │
│  ┌─────────────┐   ┌─────────────────┐  │
│  │   Worker    │   │   ASP.NET Core  │  │
│  │  (Hangfire) │   │   (Web UI/API)  │  │
│  └──────┬──────┘   └────────┬────────┘  │
│         │                   │           │
└─────────┼───────────────────┼───────────┘
          │                   │
          ▼                   ▼
    ┌───────────┐       ┌──────────┐
    │ postgres  │       │  browser │
    └───────────┘       └──────────┘
          ▲
          │
    ┌─────┴──────┐
    │  volumes   │
    │  /repos    │  ← git clones
    │  /data     │  ← postgres data
    └────────────┘
```

Outbound connections from the `gloss` container:
- Git platform REST API (fetch MRs, post comments)
- Git platform over HTTPS (clone/fetch repos)
- LLM provider API (generate reviews, update projection)

### Service Structure

The `gloss` container runs a single .NET process hosting both:

- **Hangfire background worker** — handles all jobs: polling, cloning, reviewing, projection updates. Jobs are persisted in Postgres, so they survive restarts and are retried automatically on failure.
- **ASP.NET Core** — serves the web UI (React SPA) and a thin JSON API the UI talks to.

### Volumes

| Volume | Contents |
|---|---|
| `repos` | One git clone per repository, named by project slug |
| `postgres_data` | All application state |

---

## Data Model

### Core Entities

**MergeRequest**
Represents a single MR from the Git platform. Tracks its external ID, project, branch, title, last-seen commit SHA, current state, and timestamps.

**Repository**
Represents a Git repository Gloss has encountered. Stores the project slug, clone URL, local clone path, last fetch timestamp, and an optional `PollCron` — a Quartz cron expression that overrides the default poll schedule for this specific repository. When null, the `DEFAULT_POLL_CRON` env var is used.

**DraftComment**
One comment proposed by the LLM or added by the user. Stores file path, line number, the original LLM body, the current (possibly edited) body, reasoning trace, and state (`Draft`, `Edited`, `Deleted`, `UserAdded`). The original LLM body is never overwritten.

**ConstitutionDocument**
A named document containing explicit review guidelines or standards. Stores a title, ordered position, and body. Multiple documents are supported. These are injected into every review context unchanged and are never affected by the learning loop.

**ReviewerProjection**
The current distilled model of the reviewer's personal style, learned from editing behaviour. Stored as free text with a version counter and timestamp. All previous versions are retained. The initial version can be seeded from the constitution documents.

### Event Log

All events are immutable and append-only. Each carries a UTC timestamp, the MR ID, and the comment ID where applicable.

| Event | Payload |
|---|---|
| `CommentAccepted` | comment ID, final body |
| `CommentEdited` | comment ID, body before, body after, optional reason |
| `CommentDeleted` | comment ID, body that was deleted, optional reason |
| `CommentAdded` | comment ID, full body written by the user, optional reason |
| `MRPublished` | MR ID, count of comments posted |
| `ProjectionUpdated` | new projection version |

The `reason` field is free text the user can optionally provide when taking an action. It exists specifically to prevent the projection engine from drawing the wrong generalisation from a correct but context-specific decision. For example, deleting a comment about missing pagination without a reason might teach the LLM that pagination is unimportant — adding a reason like *"this endpoint is capped at 10 items by business rule"* teaches it the correct, scoped lesson instead.

---

## Tech Stack

| Concern | Choice | Rationale |
|---|---|---|
| Runtime | .NET 10 | Familiar, strong async/worker support, single binary |
| Background jobs | Hangfire + Hangfire.PostgreSql | Persistent job queue with retries, dashboarding, proven reliability |
| Web framework | ASP.NET Core | Co-located with the worker in one process, minimal overhead |
| Frontend | React | Wide ecosystem, `diff2html` for diff rendering |
| Diff rendering | `diff2html` | Handles unified diffs with syntax highlighting out of the box |
| Database | PostgreSQL | Event sourcing benefits from proper ACID; better than SQLite for append-heavy workloads and complex projection queries |
| ORM | EF Core | Code-first migrations, clean entity mapping |
| Containerisation | Docker Compose | Two services, two volumes, one command to start |
| Git operations | `LibGit2Sharp` | Managed .NET bindings for clone/fetch; no shell-out needed |
| First-class Git provider | GitLab REST API | `IGitProvider` interface; GitHub is a second adapter |
| First-class LLM provider | Anthropic SDK (Claude) | `IReviewProvider` interface; OpenAI/Ollama are further adapters |

---

## Configuration

Gloss is configured through a first-run setup wizard that appears the first time you open the UI. All sensitive values — Git platform tokens, LLM API keys, and provider URLs — are entered there and stored encrypted in the database. Nothing sensitive lives in environment variables or config files on disk.

The setup wizard collects:

- **Git provider** — platform type (GitLab, GitHub), base URL, and personal access token
- **Projects** — the list of repositories to watch
- **LLM provider** — provider type, API key, and model
- **Poll schedule** — default cron expression used for all repositories unless overridden per-repository from the Repositories page

Once setup is complete, Gloss begins polling immediately. All config remains editable at any time through the **Config** page in the UI — rotate keys, add or remove projects, switch providers, or adjust the poll schedule without restarting the container.

### GitLab token scopes

The minimum required scopes for a GitLab Personal Access Token:

| Scope | Purpose |
|---|---|
| `read_api` | List MRs, fetch diffs |
| `read_repository` | Clone and fetch repositories |
| `write_notes` | Post comments to MRs |

If you only want to review locally and never publish, `read_api` and `read_repository` are sufficient.

---

## Getting Started

```bash
git clone <this-repo>
cd gloss
docker compose up -d
```

Open `http://localhost:5000` — the setup wizard will guide you through the rest.

---

## Development

The solution is structured as follows:

```
Gloss.sln
├── Gloss.Web/            ← ASP.NET Core API + React SPA
├── Gloss.Core/           ← Domain entities, interfaces, event types
├── Gloss.Infrastructure/ ← EF Core, Postgres, git operations, provider adapters, Hangfire jobs (poller, reviewer, projection engine)
└── Gloss.Tests/          ← Integration tests (Testcontainers for Postgres)
```

To run locally without Docker:

```bash
# start postgres only
docker compose up postgres -d

# run the app
dotnet run --project Gloss.Web
```

---

## Notes & Future Improvements

**Provider abstraction**
Both the Git platform and the LLM are behind interfaces — `IGitProvider` and `IReviewProvider`. The first-class implementations ship as GitLab and Anthropic, but adding GitHub or an OpenAI-compatible provider is a matter of writing a new adapter against the existing interface. The LLM interface treats reasoning output as optional, so providers that don't support it work without degradation.

**Projection update triggers**
Currently the projection updates after each publish. A secondary nightly trigger is worth adding for cases where events accumulate but no MR is published for an extended period.

**Repo disk usage**
Each repository is cloned in full and kept up to date via `git fetch`. For large monorepos this volume can grow significantly. Sparse checkout is a natural future optimisation once the core is stable.

**Context window limits**
For very large MRs or files, the review context may exceed the LLM's context window. A chunking strategy — reviewing files independently and merging results — is the planned mitigation.

**Context Per Project**
Save context per project to not read the source code everytime

**Configurable Prompt**
Appsettings value to configure the prompt sent to claude sdk

**Support for gitlab CICD trigger**
Claude auto pushes Reviews, separate from your reviews

**Force pull MRs**

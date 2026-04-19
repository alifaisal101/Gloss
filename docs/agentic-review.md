# Agentic Code Review

## Overview

Instead of receiving only a diff, Claude is given tools to navigate the full repository — reading files, listing directories, and searching for symbols. It decides what context it needs, gathers it, and submits a review when ready. This mirrors how a human reviewer works: they look at the diff, explore the surrounding code, then comment.

The feature replaces the single-shot diff-only review with a multi-turn tool-use loop.

---

## Tool Surface

Claude has four tools:

| Tool | Input | Output |
|---|---|---|
| `read_file` | `path: string` | file contents or "File not found" |
| `list_directory` | `path: string` | newline-separated list of entries |
| `search_code` | `pattern: string` | newline-separated matching lines with file:line prefix |
| `submit_review` | `comments: Comment[]` | terminates the loop |

`submit_review` is the only way to end the loop successfully. It accepts an array of comment objects:

```json
{
  "comments": [
    {
      "file_path": "src/Foo.cs",
      "line": 42,
      "body": "This can throw if the list is empty.",
      "reasoning": "LINQ First() throws InvalidOperationException on empty sequences."
    }
  ]
}
```

All paths are relative to the repository root. Absolute paths and `..` traversal are rejected — the tool returns "File not found" without revealing the reason.

---

## Loop Mechanics

```
POST /api/merge-requests/{id}/review
  │
  ├─ EnsureReadyAsync  →  local clone path
  │
  └─ AnthropicReviewProvider.ReviewAsync(ReviewContext)
       │
       ├─ Build system prompt  (diff + constitution)
       ├─ messages = [user: "Review this diff..."]
       │
       └─ loop (up to MaxToolCalls = 20):
            │
            ├─ ClaudeApiClient.SendAsync(system, messages, tools)
            │     ← assistant: content blocks (text, tool_use, ...)
            │
            ├─ if stop_reason != "tool_use"  →  exit with []
            │
            ├─ for each tool_use block:
            │     ├─ "submit_review"  →  parse comments, return, exit loop
            │     ├─ "read_file"      →  ReviewFileSystem.ReadFile(repoPath, path)
            │     ├─ "list_directory" →  ReviewFileSystem.ListDirectory(repoPath, path)
            │     └─ "search_code"    →  ReviewFileSystem.SearchCode(repoPath, pattern)
            │
            ├─ append assistant message to messages
            └─ append user message with tool_result(s) to messages
```

If the loop ends without a `submit_review` (max iterations reached or unexpected stop reason), the review completes with no comments.

---

## Context Structure

Each `ClaudeApiClient.SendAsync` call passes:

- **System prompt**: static instructions + diff inline, cached via `anthropic-beta: prompt-caching-2024-07-31` on the first exchange
- **Messages**: growing list of `{role, content}` objects — the conversation so far
- **Tools**: fixed list of four tool definitions (schema does not change between turns)

The diff is included in the system prompt (not the first user message) so it benefits from prompt caching across turns.

---

## Security

- All file operations go through `IReviewFileSystem`
- `RepoFileSystem` resolves every path with `Path.GetFullPath` and rejects anything that escapes the repo root
- No shell execution — `search_code` uses in-process ripgrep bindings or file enumeration
- Claude cannot write files, run commands, or access the network

---

## Boundaries

```
Application
  IReviewProvider          ← called by ReviewMergeRequestHandler
  ReviewContext            ← Diff + RepoPath
  IClaudeApiClient         ← sends messages, returns response
  IReviewFileSystem        ← read_file / list_directory / search_code
  ClaudeApiTypes           ← message and content block records

Infrastructure
  AnthropicReviewProvider  ← loop orchestrator, implements IReviewProvider
  AnthropicApiClient       ← HTTP wrapper, implements IClaudeApiClient
  RepoFileSystem           ← path-safe file access, implements IReviewFileSystem
```

---

## Key Design Decisions

**Why `submit_review` instead of detecting end-of-turn?**
A dedicated terminal tool makes intent explicit. Claude cannot accidentally end the loop — it must consciously decide it has enough context. This also lets us validate the output schema before accepting it.

**Why put the diff in the system prompt?**
The diff doesn't change between turns. Placing it in the system prompt enables prompt caching, reducing latency and cost for multi-turn reviews.

**Why cap at 20 tool calls?**
Prevents runaway loops when Claude is confused. 20 calls is enough to read ~10 files (read + context) for any realistic review. If the loop exits early due to the cap, the review completes with empty comments rather than failing noisily.

**Why `IReviewFileSystem` instead of direct file I/O in the provider?**
Testability and path-safety isolation. The provider's logic is fully testable without a real filesystem. The security invariant (no path traversal) lives entirely in `RepoFileSystem`, a single small class.

# Unity Editor connection

Use `/opt/homebrew/bin/unity` and target this project explicitly when using Unity MCP.

Run live Unity CLI commands (`status`, `pipeline list`, `list`, `command`, and
standalone MCP verification) outside the Codex execution sandbox, using the normal
command approval mechanism. On this
macOS setup, sandboxed process enumeration is denied. Unity CLI 1.0.0-beta.13 can
then remove `Library/Pipeline/.unity-pipeline-port` for an Editor that is still
running, leaving its HTTP listener alive but undiscoverable. Do not run a
sandboxed Pipeline discovery command as a fallback after a successful connection.

If discovery disappears while Unity still listens on port 7800, inspect the
descriptor's existence and the Editor log before changing project code. Restore
discovery through `Window > Pipeline > Stop Server`, then `Start Server` in the
existing Editor. This preserves unsaved scenes. Recheck CLI status and MCP scene
and Console reads outside the sandbox. Do not disable sandbox/security settings
or edit the descriptor's authentication token.

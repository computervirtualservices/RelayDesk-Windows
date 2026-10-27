# Security policy

RelayDesk is **early-stage, uncompiled and not production-verified** software.

## Reporting security concerns

Do not post stream keys, tokens, full FFmpeg command lines or unredacted logs in public issues.
Use GitHub's private vulnerability reporting feature if enabled by the repository owner.
If it is not enabled, open a public issue **without sensitive details** asking for a private reporting channel.

## Operational safeguards

- RelayDesk encrypts saved keys using Windows DPAPI for the current user. Keys may still be visible in command-line arguments of active FFmpeg processes to privileged local users.
- Its input RTMP listener binds to 127.0.0.1. Do not expose port 1935 over the internet.
- Install FFmpeg and MediaMTX only from trusted verified sources. Neither binary is included in this repository.
- Do not commit `settings.json` or any service stream keys.
- Never claim a public broadcast is live merely because FFmpeg shows outgoing frames.

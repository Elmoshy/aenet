# Security policy

## Reporting a vulnerability
Please do not open a public issue for security problems. Use **GitHub → Security → Report a vulnerability**
(private advisory) on this repository. Include the version, steps to reproduce and the impact.
We aim to answer within 7 days.

## Things that are intentionally less strict (read before you enable them)
- **Trust any certificate / auto trust**: skips or relaxes server certificate checks for one server.
  Anyone on the network path can read or change that traffic. Prefer **Pin certificate**.
- **Pin certificate** trusts the certificate the server shows at the moment you pin it. Do it on a network you trust.
- Server lists (links, passwords, UUIDs) are stored on your device. On Windows: `%APPDATA%\BatV2\servers.json`.
  Never attach this file to an issue.

## Supported versions
Only the latest release receives fixes.

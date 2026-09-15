#!/usr/bin/env python3
"""Rebuild every IsAotCompatible module and diff its trim-analyzer warnings
against the checked-in baseline. Fails only on a warning not already in the
baseline (a regression). See .github/aot-trim-warnings-baseline.json for the
update process.
"""
import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
BASELINE_PATH = REPO_ROOT / ".github" / "aot-trim-warnings-baseline.json"

MODULES = [
    "src/Chatter.CQRS/src/Chatter.CQRS/Chatter.CQRS.csproj",
    "src/Chatter.MessageBrokers/src/Chatter.MessageBrokers/Chatter.MessageBrokers.csproj",
    "src/Chatter.MessageBrokers.AzureServiceBus/src/Chatter.MessageBrokers.AzureServiceBus/Chatter.MessageBrokers.AzureServiceBus.csproj",
    "src/Chatter.MessageBrokers.AzureServiceBus.Auth/src/Chatter.MessageBrokers.AzureServiceBus.Auth/Chatter.MessageBrokers.AzureServiceBus.Auth.csproj",
    "src/Chatter.MessageBrokers.Reliability.EntityFramework/src/Chatter.MessageBrokers.Reliability.EntityFramework/Chatter.MessageBrokers.Reliability.EntityFramework.csproj",
    "src/Chatter.MessageBrokers.SqlServiceBroker/src/Chatter.MessageBrokers.SqlServiceBroker/Chatter.MessageBrokers.SqlServiceBroker.csproj",
    "src/Chatter.SqlChangeFeed/src/Chatter.SqlChangeFeed/Chatter.SqlChangeFeed.csproj",
    "src/Chatter.MessageBrokers.RabbitMQ/src/Chatter.MessageBrokers.RabbitMQ/Chatter.MessageBrokers.RabbitMQ.csproj",
    "src/Chatter.MessageBrokers.Reliability.Cosmos/src/Chatter.MessageBrokers.Reliability.Cosmos/Chatter.MessageBrokers.Reliability.Cosmos.csproj",
]

WARNING_PATTERN = re.compile(r"([^\s/][^/\\]*?\.cs)\((\d+)(?:,\d+)?\)\s*:\s*warning\s+(IL\d+)\b(.*)$")


def build_and_collect(dotnet_exe: str, packages_dir: str | None) -> list[dict]:
    """Rebuild every module and collect distinct (file, code, line) warnings."""
    entries: dict[tuple[str, str, str], str] = {}
    for module in MODULES:
        cmd = [dotnet_exe, "build", module, "-f", "net10.0", "-t:Rebuild", "-tl:false"]
        if packages_dir:
            cmd += ["--packages", packages_dir]
        result = subprocess.run(
            cmd,
            cwd=REPO_ROOT,
            capture_output=True,
            text=True,
        )
        if result.returncode != 0:
            # A plain trim-analyzer warning never fails the build on its own (not
            # treated as an error here) — a non-zero exit means a real build/restore
            # failure, which must not be silently read as "zero warnings."
            print(f"::error::Build of {module} failed — see output below", file=sys.stderr)
            print(result.stdout, file=sys.stderr)
            print(result.stderr, file=sys.stderr)
            sys.exit(2)
        for line in (result.stdout + result.stderr).splitlines():
            m = WARNING_PATTERN.search(line)
            if not m:
                continue
            file_name, line_no, code, rest = m.groups()
            key = (file_name, code, line_no)
            if key not in entries:
                msg = re.split(r"\s*\[", rest.strip().lstrip(":").strip())[0]
                entries[key] = msg[:200]
    return [
        {"file": f, "code": c, "line": int(l), "message": entries[(f, c, l)]}
        for (f, c, l) in sorted(entries.keys())
    ]


def load_baseline() -> list[dict]:
    with open(BASELINE_PATH) as f:
        return json.load(f)["warnings"]


def to_key(entry: dict) -> tuple[str, str, int]:
    return (entry["file"], entry["code"], entry["line"])


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet", help="Path to the dotnet executable to build with")
    parser.add_argument("--packages", default=None, help="Isolated NuGet packages directory (--packages passed to dotnet build); avoids stale-cache content-hash mismatches (NU1403) against a shared local/global cache")
    parser.add_argument("--update-baseline", action="store_true", help="Rewrite the baseline from the current build output instead of checking it")
    args = parser.parse_args()

    print("Rebuilding all IsAotCompatible modules (this takes a few minutes)...", file=sys.stderr)
    current = build_and_collect(args.dotnet, args.packages)

    if args.update_baseline:
        with open(BASELINE_PATH) as f:
            doc = json.load(f)
        doc["warnings"] = current
        with open(BASELINE_PATH, "w") as f:
            json.dump(doc, f, indent=2)
            f.write("\n")
        print(f"Baseline updated: {len(current)} warnings.")
        return 0

    baseline = load_baseline()
    baseline_keys = {to_key(e) for e in baseline}
    current_keys = {to_key(e) for e in current}

    new_warnings = [e for e in current if to_key(e) not in baseline_keys]
    fixed_warnings = [e for e in baseline if to_key(e) not in current_keys]

    if fixed_warnings:
        print(f"::notice::{len(fixed_warnings)} baseline warning(s) no longer present (improvement — remove from baseline when confirmed):")
        for e in fixed_warnings:
            print(f"  FIXED  {e['file']}({e['line']}) {e['code']}: {e['message']}")

    if new_warnings:
        print(f"::error::{len(new_warnings)} NEW trim-analyzer warning(s) not in the baseline — this is a regression:")
        for e in new_warnings:
            print(f"  NEW  {e['file']}({e['line']}) {e['code']}: {e['message']}")
        print(
            "\nIf this is a genuine, permanent limitation (not a bug), add it to "
            f"{BASELINE_PATH.relative_to(REPO_ROOT)} — see that file's 'readme' field "
            "for the criteria. Otherwise, fix the annotation."
        )
        return 1

    print(f"OK: {len(current)} warnings, all present in the baseline. No regressions.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

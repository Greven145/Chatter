#!/usr/bin/env python3
"""Rebuild every IsAotCompatible module and diff its trim-analyzer warnings
against the checked-in baseline (.github/aot-trim-warnings-baseline.json).
Fails only on a warning not already in the baseline (a regression).

Warning identity is (repo-relative file path, warning code, line number).

Known tradeoff of keying on the line number: an unrelated edit that shifts
lines in a file with baselined warnings makes those warnings read as NEW (and
the old entries as FIXED), so the check fails until the baseline is updated
with --update-baseline. This is accepted because the alternatives are weaker:
- a bare count hides one warning silently swapping for another;
- (file, code, message) without the line collapses distinct call sites that
  raise the same warning in one file, so fixing one and adding another reads
  as no change.
The failure output says when NEW and FIXED entries pair up by file and code,
which is the signature of shifted lines rather than a real regression.
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

WARNING_PATTERN = re.compile(
    r"^\s*(?P<path>.+?\.cs)\((?P<line>\d+)(?:,\d+)?\)\s*:\s*warning\s+(?P<code>IL\d+)\b(?P<rest>.*)$"
)


def relative_path(path: str, repo_root: Path) -> str:
    """Repo-relative POSIX path, so identical basenames in different modules stay distinct."""
    p = Path(path)
    try:
        return p.relative_to(repo_root).as_posix()
    except ValueError:
        return p.as_posix()


def parse_warnings(text: str, repo_root: Path) -> dict[tuple[str, str, int], str]:
    """Parse dotnet build output into {(relative file, code, line): message}."""
    entries: dict[tuple[str, str, int], str] = {}
    for line in text.splitlines():
        m = WARNING_PATTERN.match(line)
        if not m:
            continue
        key = (relative_path(m.group("path"), repo_root), m.group("code"), int(m.group("line")))
        if key not in entries:
            msg = re.split(r"\s*\[", m.group("rest").strip().lstrip(":").strip())[0]
            entries[key] = msg[:200]
    return entries


def build_and_collect(dotnet_exe, packages_dir, repo_root=REPO_ROOT, modules=None, runner=subprocess.run):
    """Rebuild every module and collect distinct warnings."""
    entries: dict[tuple[str, str, int], str] = {}
    for module in modules if modules is not None else MODULES:
        cmd = [dotnet_exe, "build", module, "-f", "net10.0", "-t:Rebuild", "-tl:false"]
        if packages_dir:
            cmd += ["--packages", packages_dir]
        result = runner(cmd, cwd=repo_root, capture_output=True, text=True)
        if result.returncode != 0:
            # A trim-analyzer warning never fails the build on its own (not treated
            # as an error here) — a non-zero exit means a real build/restore
            # failure, which must not be read as "zero warnings."
            print(f"::error::Build of {module} failed — see output below", file=sys.stderr)
            print(result.stdout, file=sys.stderr)
            print(result.stderr, file=sys.stderr)
            sys.exit(2)
        for key, msg in parse_warnings(result.stdout + "\n" + result.stderr, repo_root).items():
            entries.setdefault(key, msg)
    return to_entry_list(entries)


def to_entry_list(entries):
    return [
        {"file": f, "code": c, "line": l, "message": entries[(f, c, l)]}
        for (f, c, l) in sorted(entries.keys())
    ]


def to_key(entry: dict) -> tuple[str, str, int]:
    return (entry["file"], entry["code"], entry["line"])


def find_uncovered_modules(repo_root: Path, modules) -> tuple[list[str], list[str]]:
    """Return (IsAotCompatible csprojs missing from modules, listed modules that do not exist)."""
    listed = {m for m in modules}
    found = set()
    for csproj in (repo_root / "src").rglob("*.csproj"):
        rel = csproj.relative_to(repo_root).as_posix()
        parts = rel.split("/")
        if "tests" in parts or "bin" in parts or "obj" in parts:
            continue
        if "IsAotCompatible" in csproj.read_text(encoding="utf-8", errors="replace"):
            found.add(rel)
    missing_from_list = sorted(found - listed)
    stale = sorted(m for m in listed if not (repo_root / m).exists())
    return missing_from_list, stale


def evaluate(current: list[dict], baseline: list[dict]):
    """Return (exit_code, output lines)."""
    baseline_keys = {to_key(e) for e in baseline}
    current_keys = {to_key(e) for e in current}
    new_warnings = [e for e in current if to_key(e) not in baseline_keys]
    fixed_warnings = [e for e in baseline if to_key(e) not in current_keys]
    out = []

    if fixed_warnings:
        out.append(f"::notice::{len(fixed_warnings)} baseline warning(s) no longer present (improvement — remove from baseline when confirmed):")
        for e in fixed_warnings:
            out.append(f"  FIXED  {e['file']}({e['line']}) {e['code']}: {e['message']}")

    if new_warnings:
        out.append(f"::error::{len(new_warnings)} NEW trim-analyzer warning(s) not in the baseline — this is a regression:")
        for e in new_warnings:
            out.append(f"  NEW  {e['file']}({e['line']}) {e['code']}: {e['message']}")
        shifted = {(e["file"], e["code"]) for e in new_warnings} & {(e["file"], e["code"]) for e in fixed_warnings}
        if shifted:
            out.append(
                "\nNEW and FIXED entries share a file and code — likely an unrelated edit shifted "
                "line numbers rather than a real regression. Review, then run "
                "check_aot_trim_warnings.py --update-baseline."
            )
        out.append(
            "\nIf this is a genuine, permanent limitation (not a bug), add it to the baseline — "
            "see the baseline's 'readme' field for the criteria. Otherwise, fix the annotation."
        )
        return 1, out

    out.append(f"OK: {len(current)} warnings, all present in the baseline. No regressions.")
    return 0, out


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dotnet", default="dotnet", help="Path to the dotnet executable to build with")
    parser.add_argument("--packages", default=None, help="Isolated NuGet packages directory; avoids stale-cache content-hash mismatches (NU1403) against a shared cache")
    parser.add_argument("--update-baseline", action="store_true", help="Rewrite the baseline from the current build output instead of checking it")
    args = parser.parse_args()

    missing, stale = find_uncovered_modules(REPO_ROOT, MODULES)
    if missing or stale:
        for m in missing:
            print(f"::error::{m} sets IsAotCompatible but is not in MODULES in check_aot_trim_warnings.py — its warnings would go unchecked.")
        for m in stale:
            print(f"::error::{m} is in MODULES but does not exist.")
        return 2

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

    with open(BASELINE_PATH) as f:
        baseline = json.load(f)["warnings"]
    code, lines = evaluate(current, baseline)
    print("\n".join(lines))
    return code


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""Render the Chatter.Aot.Smoke.Tests TRX results as a GitHub Actions job-summary table.

Cross-references each test's outcome against the "AotStatus=KnownGap" trait membership
(supplied separately, since this runner's TRX output does not carry trait/category data)
so a reader can immediately see which failures are documented AOT/trim gaps versus a
genuine new regression.

Usage: summarize_aot_smoke.py <results.trx> <known-gap-tests.json>
"""
import json
import sys
import xml.etree.ElementTree as ET

TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: summarize_aot_smoke.py <results.trx> <known-gap-tests.json>", file=sys.stderr)
        return 2

    trx_path, known_gap_path = sys.argv[1], sys.argv[2]

    with open(known_gap_path, encoding="utf-8") as f:
        # The runner's -list output is normally a single JSON array line, but defend against any
        # stray trailing output (e.g. an ANSI reset code) by decoding only the leading JSON value.
        raw = f.read()
        known_gap_names = set(json.JSONDecoder().raw_decode(raw)[0])

    tree = ET.parse(trx_path)
    results = tree.getroot().find(f"{TRX_NS}Results")
    tests = []
    if results is not None:
        for result in results.findall(f"{TRX_NS}UnitTestResult"):
            tests.append((result.get("testName", "<unknown>"), result.get("outcome", "<unknown>")))
    tests.sort(key=lambda t: t[0])

    total = len(tests)
    passed = sum(1 for _, outcome in tests if outcome == "Passed")
    failed = sum(1 for _, outcome in tests if outcome != "Passed")
    known_gap_failed = [
        name for name, outcome in tests if outcome != "Passed" and name in known_gap_names
    ]
    unexpected_failed = [
        name for name, outcome in tests if outcome != "Passed" and name not in known_gap_names
    ]

    lines = []
    lines.append("## AOT Smoke Test Results (Native AOT, net10.0)")
    lines.append("")
    lines.append(
        f"**{passed}/{total} passed.** "
        f"{len(unexpected_failed)} unexpected failure(s) (blocking), "
        f"{len(known_gap_failed)} known-gap failure(s) (non-blocking, flagged for review)."
    )
    lines.append("")
    lines.append("| Test | Outcome | AOT Status |")
    lines.append("|---|---|---|")
    for name, outcome in tests:
        is_known_gap = name in known_gap_names
        status_label = "Known Gap" if is_known_gap else "—"
        if outcome == "Passed":
            outcome_label = "✅ Passed"
        elif is_known_gap:
            outcome_label = "⚠️ Failed (known-gap assertion changed — investigate)"
        else:
            outcome_label = "❌ Failed"
        lines.append(f"| `{name}` | {outcome_label} | {status_label} |")
    lines.append("")

    if unexpected_failed:
        lines.append(
            "Required check is **failing**: the above untagged test(s) did not match today's "
            "documented behavior."
        )
    elif known_gap_failed:
        lines.append(
            "Required check is **passing** (only `AotStatus=KnownGap` tests deviated). A known-gap "
            "test failing means its underlying behavior changed since it was written — update the "
            "test's assertion (and drop the trait if the gap actually closed) in the PR that changed it."
        )
    else:
        lines.append("Required check is **passing**; no known-gap deviations observed either.")

    print("\n".join(lines))
    return 1 if unexpected_failed else 0


if __name__ == "__main__":
    sys.exit(main())

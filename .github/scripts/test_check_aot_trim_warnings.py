import subprocess
import tempfile
import unittest
from pathlib import Path

import check_aot_trim_warnings as gate

ROOT = Path("/repo")


def line(path, ln, code="IL2026", col=5):
    return (
        f"{path}({ln},{col}): warning {code}: Using member 'X.Y()' which has "
        f"'RequiresUnreferencedCodeAttribute' can break functionality "
        f"[/repo/src/M/M.csproj::TargetFramework=net10.0]"
    )


def entry(file, code, ln, msg="m"):
    return {"file": file, "code": code, "line": ln, "message": msg}


class Completed:
    def __init__(self, returncode, stdout="", stderr=""):
        self.returncode, self.stdout, self.stderr = returncode, stdout, stderr


class ParseTests(unittest.TestCase):
    def test_parses_real_dotnet_warning_line(self):
        text = line("/repo/src/M/src/M/ChatterJson.cs", 22)
        parsed = gate.parse_warnings(text, ROOT)
        self.assertEqual(list(parsed), [("src/M/src/M/ChatterJson.cs", "IL2026", 22)])

    def test_ignores_non_il_warnings_and_other_lines(self):
        text = "\n".join([
            "/repo/src/M/A.cs(1,1): warning SYSLIB0021: obsolete",
            "Build succeeded.",
            "/repo/src/M/B.cs(3,4): error CS0103: nope",
        ])
        self.assertEqual(gate.parse_warnings(text, ROOT), {})

    def test_same_basename_in_different_modules_stays_distinct(self):
        text = "\n".join([
            line("/repo/src/A/src/A/Extensions.cs", 18),
            line("/repo/src/B/src/B/Extensions.cs", 18),
        ])
        self.assertEqual(len(gate.parse_warnings(text, ROOT)), 2)

    def test_duplicate_lines_from_dependency_rebuilds_dedupe(self):
        text = "\n".join([line("/repo/src/A/A.cs", 5)] * 3)
        self.assertEqual(len(gate.parse_warnings(text, ROOT)), 1)


class EvaluateTests(unittest.TestCase):
    def test_matching_baseline_passes(self):
        b = [entry("src/A.cs", "IL2026", 5)]
        code, out = gate.evaluate(list(b), b)
        self.assertEqual(code, 0)
        self.assertIn("No regressions", out[-1])

    def test_new_warning_fails(self):
        b = [entry("src/A.cs", "IL2026", 5)]
        cur = b + [entry("src/B.cs", "IL2090", 9)]
        code, out = gate.evaluate(cur, b)
        self.assertEqual(code, 1)
        self.assertTrue(any("NEW" in o and "src/B.cs" in o for o in out))

    def test_new_warning_in_module_b_not_masked_by_fix_in_module_a(self):
        b = [entry("src/A/Extensions.cs", "IL2026", 18)]
        cur = [entry("src/B/Extensions.cs", "IL2026", 18)]
        code, _ = gate.evaluate(cur, b)
        self.assertEqual(code, 1)

    def test_removed_warning_passes_with_improvement_note(self):
        b = [entry("src/A.cs", "IL2026", 5), entry("src/B.cs", "IL2026", 7)]
        code, out = gate.evaluate([b[0]], b)
        self.assertEqual(code, 0)
        self.assertTrue(any("FIXED" in o and "src/B.cs" in o for o in out))

    def test_shifted_lines_flag_likely_line_shift(self):
        b = [entry("src/A.cs", "IL2026", 5)]
        cur = [entry("src/A.cs", "IL2026", 8)]
        code, out = gate.evaluate(cur, b)
        self.assertEqual(code, 1)
        self.assertTrue(any("shifted" in o for o in out))


class BuildTests(unittest.TestCase):
    def test_non_zero_build_exit_is_not_read_as_clean(self):
        def runner(cmd, **kw):
            return Completed(1, stderr="error NU1403: hash mismatch")

        with self.assertRaises(SystemExit) as ctx:
            gate.build_and_collect("dotnet", None, ROOT, ["m.csproj"], runner)
        self.assertEqual(ctx.exception.code, 2)

    def test_stdout_and_stderr_are_joined_with_a_newline(self):
        def runner(cmd, **kw):
            return Completed(0, stdout="Build succeeded.", stderr=line("/repo/src/A/A.cs", 3))

        result = gate.build_and_collect("dotnet", None, ROOT, ["m.csproj"], runner)
        self.assertEqual([(e["file"], e["line"]) for e in result], [("src/A/A.cs", 3)])

    def test_packages_dir_is_passed_to_dotnet(self):
        seen = []

        def runner(cmd, **kw):
            seen.append(cmd)
            return Completed(0)

        gate.build_and_collect("dotnet", "/pk", ROOT, ["m.csproj"], runner)
        self.assertIn("--packages", seen[0])
        self.assertIn("/pk", seen[0])


class DriftTests(unittest.TestCase):
    def _repo(self, files):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        root = Path(tmp.name)
        for rel, text in files.items():
            p = root / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(text)
        return root

    def test_flags_aot_csproj_missing_from_modules(self):
        root = self._repo({
            "src/A/src/A/A.csproj": "<IsAotCompatible>true</IsAotCompatible>",
            "src/B/src/B/B.csproj": "<IsAotCompatible>true</IsAotCompatible>",
        })
        missing, stale = gate.find_uncovered_modules(root, ["src/A/src/A/A.csproj"])
        self.assertEqual(missing, ["src/B/src/B/B.csproj"])
        self.assertEqual(stale, [])

    def test_ignores_tests_and_non_aot_projects(self):
        root = self._repo({
            "src/A/src/A/A.csproj": "<IsAotCompatible>true</IsAotCompatible>",
            "src/A/tests/A.Tests.csproj": "<IsAotCompatible>true</IsAotCompatible>",
            "src/C/src/C/C.csproj": "<TargetFramework>net10.0</TargetFramework>",
        })
        missing, _ = gate.find_uncovered_modules(root, ["src/A/src/A/A.csproj"])
        self.assertEqual(missing, [])

    def test_flags_listed_module_that_does_not_exist(self):
        root = self._repo({"src/A/src/A/A.csproj": "<IsAotCompatible>true</IsAotCompatible>"})
        _, stale = gate.find_uncovered_modules(root, ["src/A/src/A/A.csproj", "src/Gone/Gone.csproj"])
        self.assertEqual(stale, ["src/Gone/Gone.csproj"])

    def test_real_repo_modules_list_matches_disk(self):
        missing, stale = gate.find_uncovered_modules(gate.REPO_ROOT, gate.MODULES)
        self.assertEqual((missing, stale), ([], []))


if __name__ == "__main__":
    unittest.main()

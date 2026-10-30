"""Type-check C# files against the real Unity engine/editor assemblies WITHOUT opening Unity (no license needed).

Uses the Roslyn compiler and managed DLLs bundled with the Unity 6000.0.43f1 install. It catches syntax errors,
wrong API names and wrong signatures. It does NOT run anything, and package types (XR Interaction Toolkit, AI Navigation,
Input System, TextMeshPro) are not available unless you pass a stub file that declares the few members you use.

Usage:
  python tools/unity_compile_check.py [--editor] [--ref OTHER.dll ...] FILE.cs [FILE.cs ...]
  python tools/unity_compile_check.py --editor unity/CanteenRush/Assets/Scripts/World/Editor/*.cs tools/stubs/NavMeshStub.cs
Exit code 0 = compiles. Override the install path with the UNITY_DATA env var (the .../Editor/Data folder).
"""
import glob
import os
import subprocess
import sys
import tempfile

DATA = os.environ.get("UNITY_DATA", "C:/Program Files/Unity/Hub/Editor/6000.0.43f1/Editor/Data")
args = sys.argv[1:]
editor = "--editor" in args
refs, files = [], []
i = 0
while i < len(args):
    if args[i] == "--editor":
        pass
    elif args[i] == "--ref":
        i += 1
        refs.append(args[i])
    else:
        files.extend(glob.glob(args[i]) or [args[i]])
    i += 1
if not files:
    raise SystemExit(__doc__)

mods = [f for f in glob.glob(DATA + "/Managed/UnityEngine/*.dll") if "Cecil" not in f]
rsp = ["-nologo", "-nostdlib", "-target:library", '-r:"%s/NetStandard/ref/2.1.0/netstandard.dll"' % DATA]
rsp += ['-r:"%s"' % m.replace(chr(92), "/") for m in mods]
rsp += ['-r:"%s"' % r for r in refs]
with tempfile.TemporaryDirectory() as tmp:
    rsp_path = os.path.join(tmp, "check.rsp")
    with open(rsp_path, "w") as f:
        f.write("\n".join(rsp) + "\n")
    cmd = [DATA + "/NetCoreRuntime/dotnet.exe", DATA + "/DotNetSdkRoslyn/csc.dll", "@" + rsp_path,
           "-out:" + os.path.join(tmp, "check.dll")] + files
    r = subprocess.run(cmd, capture_output=True, text=True)
    out = (r.stdout + r.stderr).strip()
    print(out if out else "OK: %d file(s) compile%s" % (len(files), " (editor APIs included; modules cover both)" if editor else ""))
    sys.exit(r.returncode)

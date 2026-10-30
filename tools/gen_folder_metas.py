"""Create .meta files for Assets/ folders, .cs scripts and .asmdef files that lack one, with deterministic GUIDs.

Unity generates random GUIDs for assets it first sees; if two people open the project
before the metas are committed, their GUIDs differ and every reference conflicts.
Run this after adding folders/scripts and commit the .meta files with them.

Usage: python tools/gen_folder_metas.py unity/CanteenRush/Assets
"""
import hashlib
import os
import sys

root = sys.argv[1] if len(sys.argv) > 1 else "unity/CanteenRush/Assets"
BACKSLASH = chr(92)
NL = chr(10)

FOLDER = NL.join([
    "fileFormatVersion: 2", "guid: {guid}", "folderAsset: yes", "DefaultImporter:",
    "  externalObjects: {{}}", "  userData: ", "  assetBundleName: ", "  assetBundleVariant: ", ""])
SCRIPT = NL.join([
    "fileFormatVersion: 2", "guid: {guid}", "MonoImporter:", "  externalObjects: {{}}", "  serializedVersion: 2",
    "  defaultReferences: []", "  executionOrder: 0", "  icon: {{instanceID: 0}}", "  userData: ",
    "  assetBundleName: ", "  assetBundleVariant: ", ""])
ASMDEF = NL.join([
    "fileFormatVersion: 2", "guid: {guid}", "AssemblyDefinitionImporter:", "  externalObjects: {{}}",
    "  userData: ", "  assetBundleName: ", "  assetBundleVariant: ", ""])


def write_meta(path, template):
    guid = hashlib.md5(("canteenrush:" + path).encode()).hexdigest()
    with open(path + ".meta", "w", newline="") as f:
        f.write(template.format(guid=guid))


created = 0
for base, dirs, files in os.walk(root):
    dirs[:] = [d for d in dirs if not d.startswith(".") and not d.endswith("~")]
    for fn in files:
        template = SCRIPT if fn.endswith(".cs") else ASMDEF if fn.endswith(".asmdef") else None
        path = os.path.join(base, fn).replace(BACKSLASH, "/")
        if template and not os.path.exists(path + ".meta"):
            write_meta(path, template)
            created += 1
    for d in dirs:
        path = os.path.join(base, d).replace(BACKSLASH, "/")
        if not os.path.exists(path + ".meta"):
            write_meta(path, FOLDER)
            created += 1
print("created", created, "metas")

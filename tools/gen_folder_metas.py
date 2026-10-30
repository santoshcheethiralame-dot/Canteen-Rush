"""Create .meta files for Assets/ folders that lack one, with deterministic GUIDs.

Unity generates random GUIDs for folders it first sees; if two people open the project
before the metas are committed, their GUIDs differ and every reference into the folder
conflicts. Run this after adding folders and commit the .meta files with them.

Usage: python tools/gen_folder_metas.py unity/CanteenRush/Assets
"""
import hashlib
import os
import sys

root = sys.argv[1] if len(sys.argv) > 1 else "unity/CanteenRush/Assets"
BACKSLASH = chr(92)
TEMPLATE = (
    "fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n"
    "  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
)
created = 0
for base, dirs, _ in os.walk(root):
    dirs[:] = [d for d in dirs if not d.startswith(".") and not d.endswith("~")]
    for d in dirs:
        path = os.path.join(base, d).replace(BACKSLASH, "/")
        meta = path + ".meta"
        if os.path.exists(meta):
            continue
        guid = hashlib.md5(("canteenrush:" + path).encode()).hexdigest()
        with open(meta, "w", newline="\n") as f:
            f.write(TEMPLATE.format(guid=guid))
        created += 1
print("created", created, "folder metas")

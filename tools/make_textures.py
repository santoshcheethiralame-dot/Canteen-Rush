"""Downscale the supplied textures (max 1024 px) into Assets/Art/Textures as T_*.jpg. Source files stay untracked."""
import os
from PIL import Image

SRC = "source/Outdoor/Textures"
OUT = "unity/CanteenRush/Assets/Art/Textures"
MAP = {
    "Copy of tile.jpeg": "T_Tile",
    "Copy of floor.png": "T_FloorBrick",
    "Copy of stone-texture.jpg": "T_Stone",
    "Copy of seating_granite.jpeg": "T_Granite",
    "Copy of green-grass-texture.jpg": "T_Grass",
    "Copy of PES_text.jpeg": "T_PesSign",
}

os.makedirs(OUT, exist_ok=True)
for src, name in MAP.items():
    im = Image.open(os.path.join(SRC, src)).convert("RGB")
    im.thumbnail((1024, 1024), Image.LANCZOS)
    im.save(os.path.join(OUT, name + ".jpg"), quality=88)
    print(name, im.size)

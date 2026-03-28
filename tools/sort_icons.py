#!/usr/bin/env python3
"""Sort extracted raw textures into categorized folders for CityEdit assets."""

import os
import shutil
import re

RAW_DIR = "tools/icons/raw"
OUT_DIR = "tools/icons"

# Surfer names for matching
SURFERS = [
    "Jake", "Tricky", "Fresh", "PrinceK", "MissMaia", "Monique",
    "Yutani", "Harini", "NinjaOne", "Noon", "Jenny", "Wei",
    "Spike", "Ella", "Jay", "Billy", "Rosalita", "Tasha",
    "Jaewoo", "Tagbot", "Lucy", "Georgie", "V3ctor", "Zara", "Lilah", "Ash"
]

def main():
    for d in ("surfers", "boards", "skins", "perks", "seasons", "ui"):
        os.makedirs(os.path.join(OUT_DIR, d), exist_ok=True)

    files = os.listdir(RAW_DIR)
    classified = 0

    for fname in sorted(files):
        if not fname.endswith(".png"):
            continue
        src = os.path.join(RAW_DIR, fname)
        fl = fname.lower()

        # --- Board SpriteAtlases (512x512, one per surfer) ---
        m = re.match(r'sactx-\d+-512x512-Crunch-SpriteAtlas_Boards_(\w+)-', fname)
        if m:
            name = m.group(1)
            dst = os.path.join(OUT_DIR, "boards", f"{name}.png")
            shutil.copy2(src, dst)
            print(f"  BOARD: {name} <- {fname}")
            classified += 1
            continue

        # --- Board default atlas ---
        m = re.match(r'sactx-\d+-1024x512-Crunch-SpriteAtlas_Boards_Default', fname)
        if m:
            dst = os.path.join(OUT_DIR, "boards", "Default.png")
            shutil.copy2(src, dst)
            print(f"  BOARD: Default <- {fname}")
            classified += 1
            continue

        # --- Perk SpriteAtlases (1024x512, one per surfer) ---
        m = re.match(r'sactx-\d+-1024x512-Crunch-SpriteAtlas_Perks_(\w+)-', fname)
        if m:
            name = m.group(1)
            dst = os.path.join(OUT_DIR, "perks", f"{name}.png")
            shutil.copy2(src, dst)
            print(f"  PERK:  {name} <- {fname}")
            classified += 1
            continue

        # --- Individual perk icons ---
        m = re.match(r'Icon_Perk_(\w+)_(\d+)\.png', fname)
        if m:
            name = m.group(1)
            idx = m.group(2)
            dst = os.path.join(OUT_DIR, "perks", f"{name}_perk{idx}.png")
            shutil.copy2(src, dst)
            print(f"  PERK:  {name}_perk{idx} <- {fname}")
            classified += 1
            continue

        # --- Surfer renders/portraits ---
        if "Render" in fname or "Illustration" in fname:
            for s in SURFERS:
                if s.lower() in fl:
                    dst = os.path.join(OUT_DIR, "surfers", fname)
                    shutil.copy2(src, dst)
                    print(f"  SURFER: {fname}")
                    classified += 1
                    break
            continue

        # --- Surfer big icons ---
        m = re.match(r'Icon_Surfer_(\w+)_big\.png', fname)
        if m:
            name = m.group(1)
            dst = os.path.join(OUT_DIR, "surfers", f"{name}_icon.png")
            shutil.copy2(src, dst)
            print(f"  SURFER ICON: {name} <- {fname}")
            classified += 1
            continue

        # --- Season banners ---
        if fname.startswith("Season_"):
            m = re.match(r'Season_[Bb]anner_(\w+?)_', fname)
            if m:
                name = m.group(1)
                dst = os.path.join(OUT_DIR, "seasons", fname)
                shutil.copy2(src, dst)
                print(f"  SEASON: {name} <- {fname}")
                classified += 1
                continue

        # --- UI icons (Icon_Large_*, Icon_Medium_*) ---
        if fname.startswith("Icon_Large_") or fname.startswith("Icon_Medium_") or fname.startswith("Icon_Missions"):
            dst = os.path.join(OUT_DIR, "ui", fname)
            shutil.copy2(src, dst)
            print(f"  UI: {fname}")
            classified += 1
            continue

    print(f"\nClassified {classified} files out of {len(files)} total")

if __name__ == "__main__":
    main()

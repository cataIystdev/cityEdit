#!/usr/bin/env python3
"""
Скрипт извлечения 2D иконок из AssetBundles Subway Surfers City.
Классифицирует текстуры по пути AssetBundle внутри APK.
Также извлекает иконки из большого SpriteAtlas (shared bundle).
"""

import argparse
import os
import sys
import zipfile
import tempfile
import re

try:
    import UnityPy
    from UnityPy.enums import ClassIDType
except ImportError:
    print("ERROR: pip install UnityPy Pillow")
    sys.exit(1)

try:
    from PIL import Image
except ImportError:
    print("ERROR: pip install Pillow")
    sys.exit(1)

ICON_SIZE = (128, 128)


def classify_bundle(entry_name: str):
    """Returns (category, friendly_name) based on APK zip entry name."""
    bp = entry_name.lower()

    # Surfer skins: art.surfers.<name>_skin*
    m = re.search(r'art\.surfers\.(\w+?)_skin(\d+)_(?:tier\d+_)?(\w+?)_assets', bp)
    if m:
        surfer = m.group(1)
        skin_name = m.group(3)
        return ("skins", f"{surfer}_{skin_name}")

    # Surfers: art.surfers.<name>_assets
    m = re.search(r'art\.surfers\.(\w+?)_assets', bp)
    if m:
        return ("surfers", m.group(1))

    # Boards: board_<name>/
    m = re.search(r'board_(\w+?)/', bp)
    if m:
        name = m.group(1)
        if name not in ("tutorial",):
            return ("boards", name)

    return (None, None)


def extract_textures_from_bundle(fpath, min_size=32):
    """Extract Texture2D objects from a Unity bundle file."""
    textures = []
    try:
        env = UnityPy.load(fpath)
    except Exception:
        return textures

    for obj in env.objects:
        if obj.type == ClassIDType.Texture2D:
            try:
                data = obj.read()
                img = data.image
                if img and img.size[0] >= min_size and img.size[1] >= min_size:
                    name = getattr(data, 'm_Name', None) or f"tex_{obj.path_id}"
                    textures.append((name, img, img.size[0] * img.size[1]))
            except Exception:
                pass
        elif obj.type == ClassIDType.Sprite:
            try:
                data = obj.read()
                img = data.image
                if img and img.size[0] >= min_size and img.size[1] >= min_size:
                    name = getattr(data, 'm_Name', None) or f"sprite_{obj.path_id}"
                    textures.append((name, img, img.size[0] * img.size[1]))
            except Exception:
                pass
    return textures


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--apk", required=True)
    parser.add_argument("--output", default="./icons")
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args()

    os.makedirs(args.output, exist_ok=True)
    for d in ("surfers", "boards", "skins", "raw"):
        os.makedirs(os.path.join(args.output, d), exist_ok=True)

    print(f"Extracting from: {args.apk}")

    stats = {"surfers": [], "boards": [], "skins": [], "raw": 0}

    with zipfile.ZipFile(args.apk, "r") as zf:
        entries = [n for n in zf.namelist() if n.startswith("assets/")]
        print(f"  {len(entries)} asset entries")

        with tempfile.TemporaryDirectory() as tmp_dir:
            # Extract all assets to tempdir first
            print("  Extracting assets to temp dir...")
            for entry in entries:
                try:
                    zf.extract(entry, tmp_dir)
                except Exception:
                    pass

            # Process each entry
            for entry_name in entries:
                fpath = os.path.join(tmp_dir, entry_name)
                if not os.path.isfile(fpath):
                    continue

                category, friendly = classify_bundle(entry_name)

                textures = extract_textures_from_bundle(fpath)

                if not textures:
                    continue

                if category and friendly:
                    # Classified bundle — save to category
                    cat_dir = os.path.join(args.output, category)
                    textures.sort(key=lambda t: t[2], reverse=True)

                    for idx, (tex_name, img, pixels) in enumerate(textures):
                        suffix = "" if idx == 0 else f"_{idx}"
                        icon = img.resize(ICON_SIZE, Image.Resampling.LANCZOS)
                        icon_path = os.path.join(cat_dir, f"{friendly}{suffix}.png")
                        icon.save(icon_path)

                        if args.verbose:
                            print(f"  ✓ [{category}] {friendly}{suffix} ({img.size[0]}x{img.size[1]}) '{tex_name}'")

                    stats[category].append(friendly)
                else:
                    # Unclassified — save to raw
                    raw_dir = os.path.join(args.output, "raw")
                    for tex_name, img, pixels in textures:
                        safe_name = re.sub(r'[^\w\-.]', '_', tex_name)
                        img.save(os.path.join(raw_dir, f"{safe_name}.png"))
                        stats["raw"] += 1

    # Summary
    print("\n" + "=" * 50)
    print("EXTRACTION SUMMARY")
    print("=" * 50)
    for cat in ("surfers", "boards", "skins"):
        cat_dir = os.path.join(args.output, cat)
        icons = [f for f in os.listdir(cat_dir) if f.endswith(".png")]
        print(f"  {cat:10s}: {len(icons)} icons ({len(stats[cat])} bundles)")
        for name in sorted(set(stats[cat])):
            print(f"              - {name}")
    print(f"  {'raw':10s}: {stats['raw']} textures")
    print("=" * 50)


if __name__ == "__main__":
    main()

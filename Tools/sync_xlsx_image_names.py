from __future__ import annotations

import shutil
import uuid
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SPRITES = ROOT / "Assets" / "GameKamiStreaming" / "Resources" / "GameKamiStreaming" / "Sprites"
DESKTOP_SOURCE = Path(r"C:\Users\user\Desktop\새 폴더")


MAPPINGS = {
    "img_keyboard_01": ["img_keyboard_01", "keyboard"],
    "img_camera_01": ["img_camera_01", "camera"],
    "img_energydrink_01": ["img_energydrink_01", "energydrink"],
    "img_box_01": ["img_box_01", "box"],
    "img_redbox_01": ["img_redbox_01", "redbox"],
    "img_follow_01": ["img_follow_01", "follow"],
    "img_watch_01": ["img_watch_01", "watcher"],
    "img_love_01": ["img_love_01", "love"],
    "img_donation_01": ["img_donation_01", "donation"],
    "img_reddonation_01": ["img_reddonation_01", "reddoination"],
    "img_subscriber_01": ["img_subscriber_01", "img_subscription_01", "subscription"],
    "img_stage1_01": ["img_stage1_01", "stage1"],
    "img_stage2_01": ["img_stage2_01", "stage2"],
    "img_stage3_01": ["img_stage3_01", "stage3"],
    "img_stage4_01": ["img_stage4_01", "stage4"],
    "img_stage5_01": ["img_stage5_01", "stage5"],
    "img_angry_01": ["img_angry_01", "kkami_appear/angry"],
    "img_confused_01": ["img_confused_01", "kkami_appear/confused"],
    "img_sad_01": ["img_sad_01", "kkami_appear/sad"],
    "img_shocked_01": ["img_shocked_01", "kkami_appear/shocked"],
    "img_superangry_01": ["img_superangry_01", "kkami_appear/super_angry"],
    "img_kkami_01": ["img_kkami_01", "kkami big star-Photoroom"],
    "img_manager_01": ["img_manager_01"],
}

EXTRA_META_TEMPLATES = {
    "vfx_boss1_01": "vfx_boss2_01",
    "vfx_boss1_02": "vfx_boss2_02",
    "vfx_kkami_01": "vfx_boss2_01",
    "vfx_boss4_1_1": "vfx_boss4_01_1",
}


def project_candidate(stem: str) -> Path | None:
    for suffix in (".png", ".jpg", ".jpeg"):
        path = SPRITES / f"{stem}{suffix}"
        if path.exists():
            return path
    return None


def external_candidate(stem: str) -> Path | None:
    if not DESKTOP_SOURCE.exists():
        return None
    for suffix in (".png", ".jpg", ".jpeg"):
        path = DESKTOP_SOURCE / f"{stem}{suffix}"
        if path.exists():
            return path
    return None


def find_source(stems: list[str]) -> Path | None:
    for stem in stems:
        path = project_candidate(stem)
        if path is not None:
            return path
        path = external_candidate(stem)
        if path is not None:
            return path
    return None


def write_meta_from_template(destination: Path, template: Path) -> bool:
    meta_path = Path(str(destination) + ".meta")
    if meta_path.exists() or not destination.exists() or not template.exists():
        return False

    lines = template.read_text(encoding="utf-8").splitlines()
    for index, line in enumerate(lines):
        if line.startswith("guid: "):
            lines[index] = f"guid: {uuid.uuid4().hex}"
            break
    meta_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return True


def create_meta(destination: Path, source: Path | None) -> bool:
    if source is not None:
        source_meta = Path(str(source) + ".meta")
        if write_meta_from_template(destination, source_meta):
            return True

    fallback = SPRITES / ("keyboard.png.meta" if destination.stem.startswith("img_") else "vfx_boss2_01.png.meta")
    return write_meta_from_template(destination, fallback)


def main():
    SPRITES.mkdir(parents=True, exist_ok=True)
    missing = []
    copied = []
    metas = []
    for target, candidates in MAPPINGS.items():
        destination = SPRITES / f"{target}.png"
        source = find_source(candidates)
        if source is None:
            missing.append(target)
            continue
        if source.resolve() != destination.resolve():
            shutil.copy2(source, destination)
        if create_meta(destination, source):
            metas.append(target)
        copied.append((target, source))

    for target, template in EXTRA_META_TEMPLATES.items():
        destination = SPRITES / f"{target}.png"
        template_path = SPRITES / f"{template}.png.meta"
        if write_meta_from_template(destination, template_path):
            metas.append(target)

    print("copied", len(copied))
    for target, source in copied:
        print(f"{target} <- {source}")
    print("metas", len(metas))
    for target in metas:
        print(f"meta {target}")
    if missing:
        print("missing", ",".join(missing))


if __name__ == "__main__":
    main()

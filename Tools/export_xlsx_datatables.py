from __future__ import annotations

import csv
import math
from pathlib import Path

import pandas as pd


ROOT = Path(__file__).resolve().parents[1]
XLSX = ROOT / "SourceData" / "kkami_datatable_ver02.xlsx"
OUT = ROOT / "Assets" / "GameKamiStreaming" / "Resources" / "GameKamiStreaming" / "DataTables"
RAW_OUT = OUT / "xlsx_export"


def clean(value):
    if value is None:
        return ""
    if isinstance(value, float) and math.isnan(value):
        return ""
    if pd.isna(value):
        return ""
    if isinstance(value, float) and value.is_integer():
        return str(int(value))
    text = str(value).strip()
    if text.endswith(".0") and text[:-2].isdigit():
        return text[:-2]
    return text


def int_value(value, default=0):
    text = clean(value)
    if not text:
        return default
    try:
        return int(float(text))
    except ValueError:
        return default


def float_value(value, default=0):
    text = clean(value)
    if not text:
        return default
    try:
        return float(text)
    except ValueError:
        return default


def bool_value(value):
    text = clean(value).lower()
    return "true" if text in {"true", "1", "o", "y", "yes"} else "false"


def read_sheet(name):
    df = pd.read_excel(XLSX, sheet_name=name, header=1, dtype=object)
    df = df.dropna(how="all")
    if len(df) > 0:
        first = [clean(v).lower() for v in df.iloc[0].tolist()]
        if any(v in {"int", "float", "string", "bool"} for v in first):
            df = df.iloc[1:]
    return df.fillna("")


def write_csv(path, header, rows):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.writer(handle)
        writer.writerow(header)
        writer.writerows(rows)


def build_maps():
    res_img = read_sheet("res_img")
    img_map = {clean(row["img_name_ID"]): clean(row["img_name"]) for _, row in res_img.iterrows()}
    return img_map


def resolve_image(image_key, img_map):
    image_key = clean(image_key)
    if image_key in img_map:
        return img_map[image_key]
    if image_key.startswith("I501") and len(image_key) == 6 and image_key[-2:].isdigit():
        stage_group = int(image_key[-2:])
        if 1 <= stage_group <= 5:
            return f"img_stage{stage_group}_01"
    return image_key


def export_raw_sheets():
    RAW_OUT.mkdir(parents=True, exist_ok=True)
    book = pd.ExcelFile(XLSX)
    for sheet in book.sheet_names:
        df = pd.read_excel(XLSX, sheet_name=sheet, header=None, dtype=object).fillna("")
        df.to_csv(RAW_OUT / f"{sheet}.csv", index=False, header=False, encoding="utf-8-sig")


def export_game_tables():
    img_map = build_maps()

    currency = read_sheet("currency")
    resource_rows = []
    for _, row in currency.iterrows():
        currency_id = int_value(row["currency_ID"])
        if currency_id <= 0:
            continue
        image_key = clean(row["img_name_ID"])
        resource_rows.append([
            currency_id,
            clean(row["currency_name"]).replace(" ", "_"),
            resolve_image(image_key, img_map),
            "",
        ])
    write_csv(OUT / "resource.csv", ["resource_id", "res_name", "resimg_id", "effect_id"], resource_rows)

    piece = read_sheet("piece")
    piece_rows = []
    for _, row in piece.iterrows():
        piece_id = int_value(row["piece_id"])
        if piece_id <= 0:
            continue
        image_key = clean(row["img_name_ID"])
        piece_rows.append([
            piece_id,
            clean(row["piece_name"]).replace(" ", "_"),
            int_value(row["currency_id"]),
            int_value(row["start_currency_amount"]),
            int_value(row["piece_hp"], 1),
            resolve_image(image_key, img_map),
            clean(row.get("vfx_name_ID", "")),
        ])

    boss = read_sheet("boss")
    for _, row in boss.iterrows():
        boss_id = int_value(row["boss_ID"])
        if boss_id <= 0:
            continue
        image_name = ""
        if boss_id == 30001:
            image_name = "vfx_boss1_01"
        elif boss_id == 30002:
            image_name = "vfx_boss2_01"
        elif boss_id == 30004:
            image_name = "boss/boss_4/boss4"
        piece_rows.append([
            boss_id,
            clean(row["boss_name"]),
            int_value(row["boss_currency_ID"]),
            int_value(row["boss_currency_amount"]),
            int_value(row["boss_hp"], 1),
            image_name,
            clean(row.get("vfx_name_ID", "")),
        ])
    write_csv(OUT / "piece.csv", ["piece_id", "piece_name", "resource_id", "resource_int", "hp_int", "pieceimg_id", "effect_id"], piece_rows)

    stage = read_sheet("stage")
    stage_rows = []
    for _, row in stage.iterrows():
        stage_id = int_value(row["stage_ID"])
        if stage_id <= 0:
            continue
        boss_id = int_value(row["boss_ID"])
        stage_rows.append([
            stage_id,
            boss_id if boss_id > 0 else "",
            30,
            float_value(row["keyboard_spawn"]),
            float_value(row["camera_spawn"]),
            float_value(row["energydrink_spawn"]),
            float_value(row["box_spawn"]),
            float_value(row["redbox_spawn"]),
            resolve_image(row["img_name_ID"], img_map),
            "",
        ])
    write_csv(
        OUT / "stage.csv",
        ["stage_id", "boss_id", "time_limit_sec", "piece_10001_weight", "piece_10002_weight", "piece_10003_weight", "piece_10004_weight", "piece_10005_weight", "stageimg_id", "effect_id"],
        stage_rows,
    )

    skilltree = read_sheet("skilltree")
    skill_rows = []
    for _, row in skilltree.iterrows():
        tile_id = int_value(row["tile_id"])
        if tile_id <= 0:
            continue
        skill_rows.append([
            tile_id,
            int_value(row["upgrade_type"]),
            float_value(row["increase_by"]),
            bool_value(row["sub_use"]),
            int_value(row["follow_amount"]),
            int_value(row["watch_amount"]),
            int_value(row["love_amount"]),
            int_value(row["donation_amount"]),
            int_value(row["reddonation_amount"]),
            "",
            "",
            "",
        ])
    write_csv(
        OUT / "skilltree.csv",
        ["tile_id", "reinforced_int", "up_int", "sub_use", "follow_int", "watcher_int", "love_int", "donation_int", "reddonation_int", "effect_id", "unlock_piece_id", "unlock_resource_id"],
        skill_rows,
    )

    chat = read_sheet("chat")
    chat_rows = []
    for _, row in chat.iterrows():
        chat_id = int_value(row["chat_ID"])
        if chat_id <= 0:
            continue
        chat_rows.append([
            chat_id,
            clean(row["chat_dialogue"]).strip('"'),
            float_value(row["chat_spawn"]),
            clean(row.get("img_name_ID", "")),
            clean(row.get("img_name_ID.1", "")),
        ])
    write_csv(OUT / "chat.csv", ["chat_id", "chat_dialogue", "chat_spawn", "viewer_img_id", "kkami_portrait_img_id"], chat_rows)

    res_vfx = read_sheet("res_vfx")
    boss = read_sheet("boss")
    effect_names = set()
    effect_rows = []
    for _, row in res_vfx.iterrows():
        vfx_name = clean(row["vfx_name"])
        if vfx_name and vfx_name not in effect_names:
            effect_names.add(vfx_name)
            effect_rows.append([vfx_name, vfx_name, ""])
    for _, row in boss.iterrows():
        for key in ("vfx_name_ID", "vfx_name_ID.1"):
            vfx_name = clean(row.get(key, ""))
            if vfx_name and vfx_name not in effect_names:
                effect_names.add(vfx_name)
                effect_rows.append([vfx_name, vfx_name, ""])
    for vfx_name in ("vfx_boss4_1", "vfx_boss4_1_1"):
        if vfx_name not in effect_names:
            effect_names.add(vfx_name)
            effect_rows.append([vfx_name, vfx_name, ""])
    write_csv(OUT / "effects.csv", ["effect_id", "effect_name", "prefab_path"], effect_rows)


def main():
    if not XLSX.exists():
        raise FileNotFoundError(XLSX)
    export_raw_sheets()
    export_game_tables()


if __name__ == "__main__":
    main()

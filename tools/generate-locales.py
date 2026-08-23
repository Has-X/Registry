import json
import sys
import time
from pathlib import Path

import requests


ROOT = Path(__file__).resolve().parents[1] / "src" / "Registry.App" / "Resources" / "locales"
BOUNDARY = "\n[[[REGISTRY_TRANSLATION_BOUNDARY]]]\n"
LANGUAGES = (
    "hu", "es", "de", "fr", "it", "pl", "pt-BR", "pt-PT", "tr", "id", "ro", "cs", "sk",
    "ru", "uk", "zh-CN", "zh-TW", "ar", "vi", "th", "hi", "ja", "ko", "nl", "el",
    "bg", "hr", "sr", "sl", "sv", "da", "fi", "nb",
)


def protect_placeholders(value: str) -> str:
    for index in range(10):
        value = value.replace(f"{{{index}}}", f"[@@{index}@@]")
    return value


def restore_placeholders(value: str) -> str:
    for index in range(10):
        value = value.replace(f"[@@{index}@@]", f"{{{index}}}")
    return value


def translate_batch(language: str, values: list[str]) -> list[str]:
    response = requests.get(
        "https://translate.googleapis.com/translate_a/single",
        params={
            "client": "gtx",
            "sl": "en",
            "tl": language,
            "dt": "t",
            "q": BOUNDARY.join(protect_placeholders(value) for value in values),
        },
        timeout=30,
    )
    response.raise_for_status()
    parts = "".join(item[0] for item in response.json()[0]).split(BOUNDARY)
    if len(parts) != len(values):
        return [translate_one(language, value) for value in values]
    return [restore_placeholders(part) for part in parts]


def translate_one(language: str, value: str) -> str:
    response = requests.get(
        "https://translate.googleapis.com/translate_a/single",
        params={
            "client": "gtx",
            "sl": "en",
            "tl": language,
            "dt": "t",
            "q": protect_placeholders(value),
        },
        timeout=30,
    )
    response.raise_for_status()
    return restore_placeholders("".join(item[0] for item in response.json()[0]))


def main() -> None:
    english_path = ROOT / "en" / "windows.json"
    english = json.loads(english_path.read_text(encoding="utf-8"))
    entries = list(english.items())

    selected_languages = tuple(sys.argv[1:]) or LANGUAGES
    unsupported = set(selected_languages) - set(LANGUAGES)
    if unsupported:
        raise ValueError(f"Unsupported locale(s): {', '.join(sorted(unsupported))}")

    for language in selected_languages:
        output = ROOT / language / "windows.json"
        existing = json.loads(output.read_text(encoding="utf-8")) if output.exists() else {}
        translated: dict[str, str] = {
            key: existing[key] for key, _ in entries if key in existing
        }
        missing_entries = [(key, value) for key, value in entries if key not in translated]
        for start in range(0, len(missing_entries), 8):
            batch = missing_entries[start : start + 8]
            for (key, _), value in zip(
                batch,
                translate_batch(language, [value for _, value in batch]),
            ):
                translated[key] = value
            time.sleep(0.03)

        output.parent.mkdir(exist_ok=True)
        output.write_text(
            json.dumps({key: translated[key] for key, _ in entries}, ensure_ascii=False, indent=2)
            + "\n",
            encoding="utf-8",
        )
        print(f"{language}: {len(translated)} strings", flush=True)


if __name__ == "__main__":
    main()

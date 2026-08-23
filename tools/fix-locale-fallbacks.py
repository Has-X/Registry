import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1] / "src" / "Registry.App" / "Resources" / "locales"
FIXES = {
    "es": {"editor.byte-count": "{0} bytes totales"},
    "sk": {"find.keys": "Kľúče", "label.keys": "Kľúče"},
    "sl": {"choice.center": "Sredina"},
    "sv": {
        "choice.center": "Centrerad",
        "choice.full": "Fullständig",
        "editor.decimal": "Decimalt",
        "editor.edit-title": "Redigera {0}",
        "editor.new-string": "Nytt strängvärde",
        "nav.journal": "Loggbok",
        "section.windows-integration": "Windows-integrering",
    },
    "da": {
        "action.find": "Søg",
        "action.send-feedback": "Send feedback til os",
        "choice.center": "Centreret",
        "editor.byte-count": "{0} byte",
        "editor.decimal": "Decimalt",
        "editor.hex-bytes": "Hexadecimale byte",
        "journal.count.many": "{0} øjebliksbilleder",
        "journal.count.one": "{0} øjebliksbillede",
        "label.type": "Datatype",
        "nav.journal": "Logbog",
        "section.windows-integration": "Windows-integration",
    },
    "fi": {"nav.journal": "Lokikirja"},
    "nb": {
        "choice.full": "Fullstendig",
        "editor.hex-bytes": "Heksadesimale byte",
        "label.type": "Datatype",
        "nav.journal": "Logg",
    },
}


for locale, replacements in FIXES.items():
    path = ROOT / locale / "windows.json"
    catalog = json.loads(path.read_text(encoding="utf-8"))
    for key, value in replacements.items():
        if key not in catalog:
            raise KeyError(f"{locale}: unknown key {key}")
        catalog[key] = value
    path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

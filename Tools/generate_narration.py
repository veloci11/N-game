#!/usr/bin/env python3
"""Generate only N'GAME's fixed public French phase announcements, offline.
Model/voices are supplied explicitly and never downloaded or copied into the game.
See Documentation/NARRATION-PUBLIQUE.md for source URLs and attribution.
"""
import argparse
import hashlib
import importlib.metadata
import json
import platform
from pathlib import Path

import numpy as np
import soundfile as sf
from kokoro_onnx import Kokoro

# Fixed public text only. Never insert a player's name, role assignment or target.
CORPUS = {
    "distribution": ("Chacun reçoit son secret. Regardez votre carte en privé, puis confirmez. La nuit commencera quand le village sera prêt.", 5.0, 8.0),
    "cupid": ("Yeux fermés. Cupidon s’éveille.", 0.0, 2.8),
    "lovers": ("Yeux fermés. Les amoureux se reconnaissent, puis se rendorment.", 0.0, 6.0),
    "guard": ("Yeux fermés. Le Garde s’éveille.", 0.0, 2.8),
    "seer": ("Yeux fermés. La Voyante s’éveille.", 0.0, 2.8),
    "wolves": ("Yeux fermés. Les loups s’éveillent.", 0.0, 2.8),
    "witch": ("Yeux fermés. La Sorcière s’éveille.", 0.0, 2.8),
    "hunter": ("Une dernière action est attendue.", 0.0, 2.8),
    "dawn": ("Le village se réveille.", 0.0, 2.8),
    "discussion": ("Le village débat. Partagez vos soupçons.", 0.0, 3.5),
    "vote": ("Le vote est ouvert. Choisissez en privé.", 0.0, 3.5),
    "verdict": ("Le verdict est affiché.", 0.0, 2.8),
    "finished": ("Le conte est terminé. Découvrez les rôles.", 0.0, 3.5),
}


def sha256(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for part in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(part)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model", type=Path, required=True)
    parser.add_argument("--voices", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[1] / "Assets/Resources/Werewolf/Narration")
    parser.add_argument("--only", choices=tuple(CORPUS), help="Regenerate one clip and merge its existing manifest")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    engine = Kokoro(str(args.model), str(args.voices))
    manifest = {
        "schema": 1, "voice": "ff_siwis", "language": "fr-fr", "sample_rate": 24000,
        "channels": 1, "subtype": "PCM_16", "peak_target_dbfs": -3.0,
        "model_sha256": sha256(args.model), "voices_sha256": sha256(args.voices),
        "model_source_url": "https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.1/kokoro-v1.0.onnx",
        "voices_source_url": "https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.1/voices-v1.0.bin",
        "generator_sha256": sha256(__file__),
        "environment": {"python": platform.python_version(), "platform": platform.platform(),
                        **{d.metadata["Name"]: d.version for d in sorted(importlib.metadata.distributions(), key=lambda d: d.metadata["Name"])}},
        "clips": [],
    }
    if args.only:
        previous_path = (Path(__file__).resolve().parents[1] / "Documentation/narration-manifest.json"
                         if args.output == parser.get_default("output") else args.output / "narration-manifest.json")
        previous = json.loads(previous_path.read_text(encoding="utf-8"))
        if any(previous[k] != manifest[k] for k in ("model_sha256", "voices_sha256")):
            raise RuntimeError("Partial generation requires the same model and voice files")
        manifest["clips"] = previous["clips"]
    for name, (text, minimum, maximum) in CORPUS.items():
        if args.only and name != args.only:
            continue
        speed = 1.05 if name == "distribution" else 1.15
        for attempt in range(4):
            samples, rate = engine.create(text, voice="ff_siwis", speed=speed, lang="fr-fr", trim=True)
            if rate != 24000 or samples.ndim != 1 or not np.isfinite(samples).all():
                raise RuntimeError(f"Invalid generated format: {name}")
            seconds = len(samples) / rate
            if minimum <= seconds <= maximum:
                break
            # Regenerate speech with a reasonable model speed; never cut speech to fit.
            speed *= seconds / (maximum - .12 if seconds > maximum else minimum + .12)
            if not .85 <= speed <= 1.4:
                raise RuntimeError(f"Revise public text: {name} needs speed {speed:.3f}")
        else:
            raise RuntimeError(f"Duration contract failed for {name}: {seconds:.3f}")
        peak = float(np.max(np.abs(samples)))
        if peak <= 0:
            raise RuntimeError(f"Silent clip: {name}")
        samples = samples * (10 ** (-3 / 20) / peak)
        # 5 ms edge fades suppress boundary clicks; they do not shorten speech.
        edge = min(120, len(samples) // 2)
        samples[:edge] *= np.linspace(0, 1, edge)
        samples[-edge:] *= np.linspace(1, 0, edge)
        path = args.output / f"{name}.wav"
        sf.write(path, samples, rate, subtype="PCM_16")
        actual, actual_rate = sf.read(path)
        info = sf.info(path)
        clip = {"name": name, "text": text, "speed": round(speed, 8), "duration_seconds": len(actual) / actual_rate,
                "minimum_seconds": minimum, "maximum_seconds": maximum,
                "peak_dbfs": round(float(20 * np.log10(np.max(np.abs(actual)))), 3),
                "rms_dbfs": round(float(20 * np.log10(np.sqrt(np.mean(actual ** 2)))) , 3),
                "clipped_samples": int(np.sum(np.abs(actual) >= 1)),
                "frames": info.frames, "sample_rate": info.samplerate, "channels": info.channels,
                "subtype": info.subtype, "sha256": sha256(path)}
        if clip["clipped_samples"] or info.channels != 1 or info.subtype != "PCM_16":
            raise RuntimeError(f"Output validation failed: {name}")
        existing = next((i for i, c in enumerate(manifest["clips"]) if c["name"] == name), None)
        if existing is None:
            manifest["clips"].append(clip)
        else:
            manifest["clips"][existing] = clip
        print(f"{name}: {clip['duration_seconds']:.3f}s, {clip['peak_dbfs']:.3f} dBFS, speed {speed:.3f}", flush=True)
    # An output override must not unexpectedly write outside its folder.
    if args.output == parser.get_default("output"):
        target = Path(__file__).resolve().parents[1] / "Documentation/narration-manifest.json"
    else:
        target = args.output / "narration-manifest.json"
    target.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Manifest: {target}")


if __name__ == "__main__":
    main()

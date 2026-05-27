"""
Сжимает все JPG/PNG в указанной папке (рекурсивно).
Максимальная сторона: MAX_SIZE px
Качество JPEG: QUALITY
Оригиналы сохраняются рядом с суффиксом .bak (или удаляются если DELETE_ORIGINALS=True)

Использование:
    python resize_images.py <путь к папке>
    python resize_images.py C:/Downloads/WordBuilder
"""

import sys
import os
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow не установлен. Установите: pip install pillow")
    sys.exit(1)

MAX_SIZE       = 900      # максимальная сторона в пикселях
QUALITY        = 80       # качество JPEG (1-95)
DELETE_ORIGINALS = False  # True = удалить .bak после обработки

EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp"}


def resize(path: Path) -> None:
    try:
        orig_size = path.stat().st_size

        # Читаем файл в память и сразу закрываем handle
        with open(path, "rb") as f:
            import io
            img = Image.open(io.BytesIO(f.read()))
            img.load()

        orig_w, orig_h = img.size

        # Пропускаем если уже маленькие
        if orig_w <= MAX_SIZE and orig_h <= MAX_SIZE and orig_size < 150_000:
            print(f"  SKIP  {path.name}  ({orig_w}×{orig_h}, {orig_size//1024}KB)")
            return

        # Резервная копия
        bak = path.with_suffix(path.suffix + ".bak")
        path.rename(bak)

        # Масштаб с сохранением пропорций
        img.thumbnail((MAX_SIZE, MAX_SIZE), Image.LANCZOS)

        # Сохраняем
        save_kwargs = {"quality": QUALITY, "optimize": True}
        if img.mode in ("RGBA", "P"):
            img = img.convert("RGB")
        img.save(path, "JPEG", **save_kwargs)

        new_size = path.stat().st_size
        print(f"  OK    {path.name}  {orig_w}×{orig_h} → {img.width}×{img.height}"
              f"  {orig_size//1024}KB → {new_size//1024}KB  (-{100*(1-new_size/orig_size):.0f}%)")

        if DELETE_ORIGINALS:
            bak.unlink()

    except Exception as e:
        print(f"  ERR   {path.name}: {e}")


def main() -> None:
    folder = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(".")
    if not folder.is_dir():
        print(f"Папка не найдена: {folder}")
        sys.exit(1)

    files = [p for p in folder.rglob("*") if p.suffix.lower() in EXTENSIONS]
    if not files:
        print("JPG/PNG файлы не найдены.")
        return

    print(f"Найдено файлов: {len(files)}\n")
    for f in files:
        resize(f)

    print("\nГотово.")


if __name__ == "__main__":
    main()

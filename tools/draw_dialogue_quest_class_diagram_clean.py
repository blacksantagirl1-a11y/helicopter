from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "dialogue_quest_class_diagram_clean.png"

W, H = 1220, 780
BG = (255, 255, 255)
BOX_FILL = (247, 250, 253)
HEADER_FILL = (238, 244, 252)
BLUE = (38, 86, 160)
LINE = (80, 80, 80)
TEXT = (0, 0, 0)


def get_font(size: int, bold: bool = False):
    candidates = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
    ]
    for candidate in candidates:
        path = Path(candidate)
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


TITLE = get_font(20, True)
BODY = get_font(15)
SMALL = get_font(14)


class Box:
    def __init__(self, name: str, x: int, y: int, w: int, h: int, lines: list[str]):
        self.name = name
        self.x = x
        self.y = y
        self.w = w
        self.h = h
        self.lines = lines

    @property
    def right(self):
        return self.x + self.w

    @property
    def bottom(self):
        return self.y + self.h


boxes = {
    "DialogueController": Box(
        "DialogueController",
        55,
        70,
        330,
        165,
        ["+RequestDialogue(DialogueEventId)", "+GetCurrentDay()", "+AdvanceDay()", "+DialogueFinished"],
    ),
    "DialogueDatabase": Box(
        "DialogueDatabase",
        445,
        70,
        330,
        165,
        ["+TryGetEntry(day, eventId, out entry)"],
    ),
    "DialogueEntry": Box(
        "DialogueEntry",
        835,
        70,
        330,
        165,
        ["+Day", "+EventId", "+PlayerCanMove", "+TimeScale", "+Lines"],
    ),
    "DialogueLineData": Box(
        "DialogueLineData",
        55,
        310,
        330,
        170,
        ["+SpeakerName", "+Text", "+QuestAction", "+QuestId"],
    ),
    "DialogueSaveService": Box(
        "DialogueSaveService",
        445,
        310,
        330,
        170,
        ["+GetCurrentDay()", "+SetCurrentDay(day)", "+AdvanceDay()"],
    ),
    "DailyQuestManager": Box(
        "DailyQuestManager",
        835,
        310,
        330,
        170,
        ["+TryActivateQuest(day, questId)", "+ReportInteraction(key, amount)", "+TryCompleteTurnIn(...)", "+NotifyDialogueFinished(...)"],
    ),
    "DailyQuestDatabase": Box(
        "DailyQuestDatabase",
        160,
        575,
        330,
        165,
        ["+TryGetQuest(day, questId, out quest)"],
    ),
    "DailyQuestDefinition": Box(
        "DailyQuestDefinition",
        700,
        575,
        330,
        165,
        ["+Day", "+QuestId", "+DisplayName", "+ObjectiveType", "+TargetItem", "+RequiredCount"],
    ),
}


def draw_box(draw: ImageDraw.ImageDraw, b: Box):
    draw.rectangle([b.x, b.y, b.right, b.bottom], fill=BOX_FILL, outline=BLUE, width=2)
    header_h = 40
    draw.rectangle([b.x, b.y, b.right, b.y + header_h], fill=HEADER_FILL, outline=BLUE, width=2)
    tw = draw.textlength(b.name, font=TITLE)
    draw.text((b.x + (b.w - tw) / 2, b.y + 8), b.name, fill=TEXT, font=TITLE)
    y = b.y + header_h + 18
    for line in b.lines:
        draw.text((b.x + 14, y), line, fill=TEXT, font=SMALL)
        y += 20


def arrow(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], width: int = 2):
    draw.line(points, fill=LINE, width=width, joint="curve")
    x1, y1 = points[-2]
    x2, y2 = points[-1]
    if abs(x2 - x1) >= abs(y2 - y1):
        direction = 1 if x2 > x1 else -1
        head = [(x2, y2), (x2 - 10 * direction, y2 - 6), (x2 - 10 * direction, y2 + 6)]
    else:
        direction = 1 if y2 > y1 else -1
        head = [(x2, y2), (x2 - 6, y2 - 10 * direction), (x2 + 6, y2 - 10 * direction)]
    draw.polygon(head, fill=LINE)


def label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int):
    pad = 4
    bbox = draw.textbbox((x, y), text, font=BODY)
    draw.rectangle([bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad], fill=BG)
    draw.text((x, y), text, fill=LINE, font=BODY)


img = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(img)

for box in boxes.values():
    draw_box(d, box)

# DialogueController relationships, routed above/between boxes.
arrow(d, [(385, 130), (445, 130)], 2)
label(d, "tra cứu hội thoại", 392, 100)

arrow(d, [(220, 235), (220, 270), (610, 270), (610, 310)], 2)
label(d, "lưu/ngày hiện tại", 430, 245)

arrow(d, [(385, 190), (410, 190), (410, 505), (835, 505), (835, 395)], 2)
label(d, "kích hoạt / báo hoàn thành", 545, 500)

# DialogueDatabase to Entry and Entry to Lines.
arrow(d, [(775, 130), (835, 130)], 2)
label(d, "trả entry", 790, 100)

arrow(d, [(1000, 235), (1000, 270), (25, 270), (25, 395), (55, 395)], 2)
label(d, "chứa danh sách dòng thoại", 60, 245)

# DialogueSaveService to quest manager through whitespace.
arrow(d, [(775, 395), (835, 395)], 2)
label(d, "ngày/nhiệm vụ", 780, 365)

# Quest manager to quest database and definition routed below middle row.
arrow(d, [(1000, 480), (1000, 535), (325, 535), (325, 575)], 2)
label(d, "tra cứu quest", 500, 510)

arrow(d, [(490, 655), (700, 655)], 2)
label(d, "trả định nghĩa quest", 520, 625)

img.save(OUT)
print(OUT)

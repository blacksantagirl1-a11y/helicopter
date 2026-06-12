from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "inventory_class_diagram_clean.png"

W, H = 1200, 760
BG = (255, 255, 255)
BOX_FILL = (247, 250, 253)
HEADER_FILL = (238, 244, 252)
BLUE = (39, 86, 160)
LINE = (80, 80, 80)
TEXT = (0, 0, 0)


def font(size: int, bold: bool = False):
    names = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
    ]
    for name in names:
        p = Path(name)
        if p.exists():
            return ImageFont.truetype(str(p), size)
    return ImageFont.load_default()


TITLE = font(20, True)
BODY = font(15)
SMALL = font(14)


class Box:
    def __init__(self, name: str, x: int, y: int, w: int, h: int, lines: list[str]):
        self.name = name
        self.x = x
        self.y = y
        self.w = w
        self.h = h
        self.lines = lines

    @property
    def left(self):
        return self.x

    @property
    def right(self):
        return self.x + self.w

    @property
    def top(self):
        return self.y

    @property
    def bottom(self):
        return self.y + self.h

    def point(self, side: str, offset: int = 0):
        if side == "left":
            return self.left, self.y + self.h // 2 + offset
        if side == "right":
            return self.right, self.y + self.h // 2 + offset
        if side == "top":
            return self.x + self.w // 2 + offset, self.top
        if side == "bottom":
            return self.x + self.w // 2 + offset, self.bottom
        raise ValueError(side)


boxes = {
    "Interactable": Box(
        "Interactable",
        60,
        70,
        320,
        150,
        ["+CanInteract", "+PromptText", "+BaseInteract(PlayerUI)", "#Interact()", "#PresentInteraction(PlayerUI)"],
    ),
    "InventoryPickup": Box(
        "InventoryPickup",
        445,
        70,
        320,
        150,
        ["-InventoryItemDefinition itemDefinition", "-int amount", "#Interact()"],
    ),
    "TreeLogPickup": Box("TreeLogPickup", 830, 70, 320, 150, []),
    "MeatPickup": Box("MeatPickup", 60, 285, 320, 150, []),
    "Door": Box("Door", 445, 285, 320, 150, []),
    "CampingCookingInteractable": Box("CampingCookingInteractable", 830, 285, 320, 150, []),
    "InventoryItemDefinition": Box(
        "InventoryItemDefinition",
        60,
        530,
        320,
        170,
        ["+ItemId", "+DisplayName", "+Description", "+Icon", "+MaxStack", "+TryUse(...)"],
    ),
    "PlayerInventory": Box(
        "PlayerInventory",
        445,
        530,
        320,
        170,
        ["+SlotCount", "+Slots", "+InventoryChanged", "+FeedbackRequested", "+ItemAdded", "+TryAddItem(...)", "+TryConsumeSlot(...)", "+TryUseSlot(...)"],
    ),
    "InventoryUIController": Box(
        "InventoryUIController",
        830,
        530,
        320,
        170,
        ["+IsInventoryOpen", "+ToggleInventory()", "+SetInventoryOpen(bool)"],
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
        y += 19


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

# Inheritance/use relationships routed through whitespace lanes.
arrow(d, [(380, 130), (410, 130), (410, 55), (830, 55), (830, 130)], 2)
label(d, "kế thừa / mở rộng", 520, 34)

arrow(d, [(380, 150), (410, 150), (410, 265), (60, 265), (60, 360)], 2)
arrow(d, [(380, 170), (420, 170), (420, 265), (445, 265), (445, 360)], 2)
arrow(d, [(380, 190), (420, 190), (420, 250), (830, 250), (830, 360)], 2)

# InventoryPickup dependencies.
arrow(d, [(605, 220), (605, 500), (605, 530)], 2)
label(d, "thêm vật phẩm", 620, 370)

arrow(d, [(445, 145), (410, 145), (410, 505), (380, 615)], 2)
label(d, "itemDefinition", 245, 500)

# PlayerInventory notifies UI through a clean horizontal lane.
arrow(d, [(765, 615), (830, 615)], 2)
label(d, "cập nhật UI", 775, 585)

# InventoryItemDefinition can be used by PlayerInventory.
arrow(d, [(445, 655), (380, 655)], 2)
label(d, "dữ liệu item", 385, 675)

img.save(OUT)
print(OUT)

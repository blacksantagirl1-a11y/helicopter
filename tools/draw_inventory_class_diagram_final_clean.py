from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "inventory_class_diagram_final_clean.png"

W, H = 1650, 1080
BG = (255, 255, 255)
BOX_FILL = (248, 251, 255)
HEADER_FILL = (238, 245, 254)
BLUE = (32, 82, 165)
BLACK = (42, 42, 42)
CALL = (0, 90, 190)
TEXT = (0, 0, 0)


def get_font(size: int, bold: bool = False):
    paths = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
    ]
    for path in paths:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


TITLE = get_font(22, True)
BODY = get_font(17)
SMALL = get_font(16)


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

    @property
    def cx(self):
        return self.x + self.w // 2

    @property
    def cy(self):
        return self.y + self.h // 2


boxes = {
    "Interactable": Box(
        "Interactable",
        560,
        50,
        430,
        215,
        ["+CanInteract", "+PromptText", "+BaseInteract(PlayerUI)", "#Interact()", "#PresentInteraction(PlayerUI)"],
    ),
    "Door": Box("Door", 95, 380, 285, 155, ["+Open()", "+Close()", "+Interact()"]),
    "CampingCookingInteractable": Box(
        "CampingCookingInteractable",
        430,
        380,
        420,
        155,
        ["+Interact()", "+EatCookedFood()", "+ReportQuestProgress()"],
    ),
    "InventoryPickup": Box(
        "InventoryPickup",
        1040,
        380,
        440,
        185,
        ["-InventoryItemDefinition itemDefinition", "-int amount", "#Interact()"],
    ),
    "MeatPickup": Box("MeatPickup", 1000, 680, 260, 145, ["#Interact()"]),
    "TreeLogPickup": Box("TreeLogPickup", 1300, 680, 280, 145, ["#Interact()"]),
    "InventoryItemDefinition": Box(
        "InventoryItemDefinition",
        80,
        820,
        385,
        205,
        ["+ItemId", "+DisplayName", "+Description", "+Icon", "+MaxStack", "+TryUse(...)"],
    ),
    "PlayerInventory": Box(
        "PlayerInventory",
        570,
        805,
        440,
        220,
        ["+SlotCount", "+Slots", "+InventoryChanged", "+ItemAdded", "+TryAddItem(...)", "+TryUseSlot(...)"],
    ),
    "InventoryUIController": Box(
        "InventoryUIController",
        1160,
        855,
        360,
        170,
        ["+IsInventoryOpen", "+ToggleInventory()", "+SetInventoryOpen(bool)"],
    ),
}


def draw_box(draw: ImageDraw.ImageDraw, b: Box):
    draw.rectangle([b.x, b.y, b.right, b.bottom], fill=BOX_FILL, outline=BLUE, width=3)
    header_h = 48
    draw.rectangle([b.x, b.y, b.right, b.y + header_h], fill=HEADER_FILL, outline=BLUE, width=3)
    tw = draw.textlength(b.name, font=TITLE)
    draw.text((b.x + (b.w - tw) / 2, b.y + 11), b.name, fill=TEXT, font=TITLE)
    y = b.y + header_h + 18
    for line in b.lines:
        draw.text((b.x + 18, y), line, fill=TEXT, font=SMALL)
        y += 23


def label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int, color=CALL):
    pad = 5
    bbox = draw.textbbox((x, y), text, font=BODY)
    draw.rectangle([bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad], fill=BG)
    draw.text((x, y), text, fill=color, font=BODY)


def solid(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=BLACK, width=2):
    draw.line(pts, fill=color, width=width, joint="curve")


def dashed(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=CALL, width=2, dash=14, gap=8):
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        dx, dy = x2 - x1, y2 - y1
        dist = (dx * dx + dy * dy) ** 0.5
        if dist == 0:
            continue
        ux, uy = dx / dist, dy / dist
        pos = 0.0
        draw_on = True
        while pos < dist:
            nxt = min(pos + (dash if draw_on else gap), dist)
            if draw_on:
                draw.line([(x1 + ux * pos, y1 + uy * pos), (x1 + ux * nxt, y1 + uy * nxt)], fill=color, width=width)
            pos = nxt
            draw_on = not draw_on


def inheritance_head(draw: ImageDraw.ImageDraw, x: int, y: int):
    pts = [(x, y), (x - 13, y + 23), (x + 13, y + 23)]
    draw.polygon(pts, fill=BG, outline=BLACK)
    draw.line([pts[0], pts[1], pts[2], pts[0]], fill=BLACK, width=2)


def arrow_head(draw: ImageDraw.ImageDraw, x: int, y: int, direction: str, color=CALL):
    if direction == "right":
        pts = [(x, y), (x - 16, y - 9), (x - 16, y + 9)]
    elif direction == "left":
        pts = [(x, y), (x + 16, y - 9), (x + 16, y + 9)]
    elif direction == "down":
        pts = [(x, y), (x - 9, y - 16), (x + 9, y - 16)]
    else:
        raise ValueError(direction)
    draw.polygon(pts, fill=color)


img = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(img)

for box in boxes.values():
    draw_box(d, box)

# Inheritance to Interactable. The shared bus sits in whitespace below Interactable.
inheritance_head(d, boxes["Interactable"].cx, boxes["Interactable"].bottom)
solid(d, [(boxes["Interactable"].cx, boxes["Interactable"].bottom + 23), (boxes["Interactable"].cx, 325)])
solid(d, [(235, 380), (235, 325), (boxes["Interactable"].cx, 325)])
solid(d, [(640, 380), (640, 325), (boxes["Interactable"].cx, 325)])
solid(d, [(1260, 380), (1260, 325), (boxes["Interactable"].cx, 325)])
label(d, "kế thừa Interactable", 710, 300, BLACK)

# Inheritance to InventoryPickup. Separate bus below InventoryPickup.
inheritance_head(d, boxes["InventoryPickup"].cx, boxes["InventoryPickup"].bottom)
solid(d, [(boxes["InventoryPickup"].cx, boxes["InventoryPickup"].bottom + 23), (boxes["InventoryPickup"].cx, 630)])
solid(d, [(1130, 680), (1130, 630), (boxes["InventoryPickup"].cx, 630)])
solid(d, [(1440, 680), (1440, 630), (boxes["InventoryPickup"].cx, 630)])
label(d, "kế thừa InventoryPickup", 1285, 605, BLACK)

# Runtime/data relationships. Each has its own lane.
dashed(d, [(1040, 465), (920, 465), (920, 735), (273, 735), (273, 820)], CALL)
arrow_head(d, 273, 820, "down", CALL)
label(d, "itemDefinition", 785, 435, CALL)

dashed(d, [(1260, 565), (1260, 760), (790, 760), (790, 805)], CALL)
arrow_head(d, 790, 805, "down", CALL)
label(d, "Interact() -> TryAddItem()", 920, 730, CALL)

dashed(d, [(640, 535), (640, 790), (695, 790), (695, 805)], CALL)
arrow_head(d, 695, 805, "down", CALL)
label(d, "dùng / tiêu hao vật phẩm", 445, 755, CALL)

dashed(d, [(1010, 930), (1160, 930)], CALL)
arrow_head(d, 1160, 930, "right", CALL)
label(d, "InventoryChanged", 1035, 895, CALL)

dashed(d, [(570, 930), (465, 930)], CALL)
arrow_head(d, 465, 930, "left", CALL)
label(d, "slot chứa itemDefinition", 355, 895, CALL)

img.save(OUT)
print(OUT)

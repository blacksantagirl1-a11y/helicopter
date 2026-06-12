from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "inventory_class_diagram_directional.png"

W, H = 1550, 940
BG = (255, 255, 255)
BOX_FILL = (248, 251, 255)
HEADER_FILL = (238, 245, 254)
BLUE = (32, 82, 165)
INHERIT = (40, 40, 40)
CALL = (0, 92, 190)
TEXT = (0, 0, 0)


def get_font(size: int, bold: bool = False):
    candidates = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
    ]
    for candidate in candidates:
        if Path(candidate).exists():
            return ImageFont.truetype(candidate, size)
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
        110,
        65,
        370,
        205,
        ["+CanInteract", "+PromptText", "+BaseInteract(PlayerUI)", "#Interact()", "#PresentInteraction(PlayerUI)"],
    ),
    "Door": Box("Door", 70, 390, 270, 145, ["+Open()", "+Close()", "+Interact()"]),
    "CampingCookingInteractable": Box(
        "CampingCookingInteractable",
        385,
        390,
        380,
        145,
        ["+Interact()", "+EatCookedFood()", "+ReportQuestProgress()"],
    ),
    "InventoryPickup": Box(
        "InventoryPickup",
        895,
        65,
        420,
        205,
        ["-InventoryItemDefinition itemDefinition", "-int amount", "#Interact()"],
    ),
    "MeatPickup": Box("MeatPickup", 835, 390, 265, 145, ["#Interact()"]),
    "TreeLogPickup": Box("TreeLogPickup", 1145, 390, 275, 145, ["#Interact()"]),
    "InventoryItemDefinition": Box(
        "InventoryItemDefinition",
        90,
        685,
        370,
        190,
        ["+ItemId", "+DisplayName", "+Description", "+Icon", "+MaxStack", "+TryUse(...)"],
    ),
    "PlayerInventory": Box(
        "PlayerInventory",
        575,
        685,
        430,
        205,
        ["+SlotCount", "+Slots", "+InventoryChanged", "+ItemAdded", "+TryAddItem(...)", "+TryUseSlot(...)"],
    ),
    "InventoryUIController": Box(
        "InventoryUIController",
        1115,
        695,
        350,
        175,
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
    for item in b.lines:
        draw.text((b.x + 18, y), item, fill=TEXT, font=SMALL)
        y += 23


def label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int, color=CALL):
    pad = 5
    bbox = draw.textbbox((x, y), text, font=BODY)
    draw.rectangle([bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad], fill=BG)
    draw.text((x, y), text, fill=color, font=BODY)


def line(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=INHERIT, width=2):
    draw.line(pts, fill=color, width=width, joint="curve")


def dashed(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=CALL, width=2, dash=14, gap=8):
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        dx, dy = x2 - x1, y2 - y1
        dist = (dx * dx + dy * dy) ** 0.5
        if dist == 0:
            continue
        ux, uy = dx / dist, dy / dist
        pos = 0.0
        on = True
        while pos < dist:
            nxt = min(pos + (dash if on else gap), dist)
            if on:
                draw.line([(x1 + ux * pos, y1 + uy * pos), (x1 + ux * nxt, y1 + uy * nxt)], fill=color, width=width)
            pos = nxt
            on = not on


def triangle_up(draw: ImageDraw.ImageDraw, x: int, y: int):
    pts = [(x, y), (x - 13, y + 23), (x + 13, y + 23)]
    draw.polygon(pts, fill=BG, outline=INHERIT)
    draw.line([pts[0], pts[1], pts[2], pts[0]], fill=INHERIT, width=2)


def arrow_head(draw: ImageDraw.ImageDraw, x: int, y: int, direction: str, color=CALL):
    if direction == "right":
        pts = [(x, y), (x - 16, y - 9), (x - 16, y + 9)]
    elif direction == "left":
        pts = [(x, y), (x + 16, y - 9), (x + 16, y + 9)]
    elif direction == "down":
        pts = [(x, y), (x - 9, y - 16), (x + 9, y - 16)]
    elif direction == "up":
        pts = [(x, y), (x - 9, y + 16), (x + 9, y + 16)]
    else:
        raise ValueError(direction)
    draw.polygon(pts, fill=color)


img = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(img)

for box in boxes.values():
    draw_box(d, box)

# Inheritance arrows: child -> parent, hollow triangle at parent.
triangle_up(d, boxes["Interactable"].cx, boxes["Interactable"].bottom)
line(d, [(205, 390), (205, 330), (boxes["Interactable"].cx - 45, 330), (boxes["Interactable"].cx - 45, boxes["Interactable"].bottom)])
line(d, [(575, 390), (575, 330), (boxes["Interactable"].cx + 45, 330), (boxes["Interactable"].cx + 45, boxes["Interactable"].bottom)])
label(d, "kế thừa", 265, 305, INHERIT)

triangle_up(d, boxes["InventoryPickup"].cx, boxes["InventoryPickup"].bottom)
line(d, [(970, 390), (970, 330), (boxes["InventoryPickup"].cx - 45, 330), (boxes["InventoryPickup"].cx - 45, boxes["InventoryPickup"].bottom)])
line(d, [(1285, 390), (1285, 330), (boxes["InventoryPickup"].cx + 45, 330), (boxes["InventoryPickup"].cx + 45, boxes["InventoryPickup"].bottom)])
label(d, "kế thừa", 1105, 305, INHERIT)

# Runtime/data interaction arrows with explicit directions.
dashed(d, [(895, 165), (720, 165), (720, 630), (290, 630), (290, 685)], CALL)
arrow_head(d, 290, 685, "down", CALL)
label(d, "tham chiếu itemDefinition", 480, 135, CALL)

dashed(d, [(1105, 270), (1105, 620), (790, 620), (790, 685)], CALL)
arrow_head(d, 790, 685, "down", CALL)
label(d, "Interact() -> TryAddItem()", 860, 590, CALL)

dashed(d, [(1005, 790), (1115, 790)], CALL)
arrow_head(d, 1115, 790, "right", CALL)
label(d, "InventoryChanged", 1015, 755, CALL)

dashed(d, [(575, 820), (460, 820)], CALL)
arrow_head(d, 460, 820, "left", CALL)
label(d, "slot dùng dữ liệu item", 365, 785, CALL)

# Small note explaining arrow types.
d.rectangle([90, 900, 750, 930], fill=BG)
d.text((95, 900), "Tam giác rỗng: kế thừa. Mũi tên xanh đứt: tương tác/tham chiếu dữ liệu trong runtime.", fill=(70, 70, 70), font=BODY)

img.save(OUT)
print(OUT)

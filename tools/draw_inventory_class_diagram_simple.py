from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "inventory_class_diagram_simple.png"

W, H = 1500, 900
BG = (255, 255, 255)
BOX_FILL = (248, 251, 255)
HEADER_FILL = (238, 245, 254)
BLUE = (32, 82, 165)
LINE = (45, 45, 45)
DASH = (0, 84, 205)
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
        120,
        70,
        360,
        205,
        ["+CanInteract", "+PromptText", "+BaseInteract(PlayerUI)", "#Interact()", "#PresentInteraction(PlayerUI)"],
    ),
    "Door": Box("Door", 80, 405, 260, 145, ["+Open()", "+Close()", "+Interact()"]),
    "CampingCookingInteractable": Box(
        "CampingCookingInteractable",
        375,
        405,
        360,
        145,
        ["+Interact()", "+EatCookedFood()", "+ReportQuestProgress()"],
    ),
    "InventoryPickup": Box(
        "InventoryPickup",
        850,
        70,
        420,
        205,
        ["-InventoryItemDefinition itemDefinition", "-int amount", "#Interact()"],
    ),
    "MeatPickup": Box("MeatPickup", 815, 405, 260, 145, ["#Interact()"]),
    "TreeLogPickup": Box("TreeLogPickup", 1110, 405, 260, 145, ["#Interact()"]),
    "InventoryItemDefinition": Box(
        "InventoryItemDefinition",
        115,
        680,
        370,
        185,
        ["+ItemId", "+DisplayName", "+Description", "+Icon", "+MaxStack", "+TryUse(...)"],
    ),
    "PlayerInventory": Box(
        "PlayerInventory",
        570,
        680,
        410,
        195,
        ["+SlotCount", "+Slots", "+InventoryChanged", "+ItemAdded", "+TryAddItem(...)", "+TryUseSlot(...)"],
    ),
    "InventoryUIController": Box(
        "InventoryUIController",
        1080,
        690,
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


def text_label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int, color=LINE):
    pad = 5
    bbox = draw.textbbox((x, y), text, font=BODY)
    draw.rectangle([bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad], fill=BG)
    draw.text((x, y), text, fill=color, font=BODY)


def solid(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=LINE, width=2):
    draw.line(pts, fill=color, width=width, joint="curve")


def dashed(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=DASH, width=2, dash_len=13, gap=8):
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        dx = x2 - x1
        dy = y2 - y1
        dist = (dx * dx + dy * dy) ** 0.5
        if not dist:
            continue
        ux = dx / dist
        uy = dy / dist
        cursor = 0.0
        on = True
        while cursor < dist:
            nxt = min(cursor + (dash_len if on else gap), dist)
            if on:
                draw.line(
                    [(x1 + ux * cursor, y1 + uy * cursor), (x1 + ux * nxt, y1 + uy * nxt)],
                    fill=color,
                    width=width,
                )
            cursor = nxt
            on = not on


def inheritance_head(draw: ImageDraw.ImageDraw, x: int, y: int):
    pts = [(x, y), (x - 13, y + 23), (x + 13, y + 23)]
    draw.polygon(pts, fill=BG, outline=LINE)
    draw.line([pts[0], pts[1], pts[2], pts[0]], fill=LINE, width=2)


def arrow_head(draw: ImageDraw.ImageDraw, x: int, y: int, direction: str, color=DASH):
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

# Group 1: interactable inheritance, routed with short local lines.
inheritance_head(d, boxes["Interactable"].cx, boxes["Interactable"].bottom)
solid(d, [(210, 405), (210, 330), (boxes["Interactable"].cx - 40, 330), (boxes["Interactable"].cx - 40, boxes["Interactable"].bottom)])
solid(d, [(555, 405), (555, 330), (boxes["Interactable"].cx + 40, 330), (boxes["Interactable"].cx + 40, boxes["Interactable"].bottom)])

# Group 2: pickup inheritance, also local and separate.
inheritance_head(d, boxes["InventoryPickup"].cx, boxes["InventoryPickup"].bottom)
solid(d, [(945, 405), (945, 330), (boxes["InventoryPickup"].cx - 45, 330), (boxes["InventoryPickup"].cx - 45, boxes["InventoryPickup"].bottom)])
solid(d, [(1245, 405), (1245, 330), (boxes["InventoryPickup"].cx + 45, 330), (boxes["InventoryPickup"].cx + 45, boxes["InventoryPickup"].bottom)])

# Dependencies/actions: one clean bottom lane, no overlaps.
dashed(d, [(850, 170), (650, 170), (650, 625), (300, 625), (300, 680)], DASH)
arrow_head(d, 300, 680, "down", DASH)
text_label(d, "itemDefinition", 500, 142, DASH)

solid(d, [(1060, 275), (1060, 620), (775, 620), (775, 680)], LINE)
arrow_head(d, 775, 680, "down", LINE)
text_label(d, "thêm vật phẩm", 905, 590, LINE)

dashed(d, [(980, 790), (1080, 790)], DASH)
arrow_head(d, 1080, 790, "right", DASH)
text_label(d, "cập nhật UI", 1000, 755, DASH)

dashed(d, [(570, 805), (485, 805)], DASH)
arrow_head(d, 485, 805, "left", DASH)
text_label(d, "slot chứa dữ liệu item", 335, 770, DASH)

img.save(OUT)
print(OUT)

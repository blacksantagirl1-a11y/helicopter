from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "inventory_class_diagram_pretty_arrows.png"

W, H = 1500, 980
BG = (255, 255, 255)
BOX_FILL = (248, 251, 255)
HEADER_FILL = (238, 245, 254)
BLUE = (32, 82, 165)
LINE = (42, 42, 42)
DASH = (0, 84, 205)
TEXT = (0, 0, 0)


def font(size: int, bold: bool = False):
    paths = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
    ]
    for path in paths:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


TITLE = font(22, True)
BODY = font(17)
SMALL = font(16)


class Box:
    def __init__(self, name: str, x: int, y: int, w: int, h: int, lines: list[str]):
        self.name = name
        self.x = x
        self.y = y
        self.w = w
        self.h = h
        self.lines = lines

    @property
    def right(self) -> int:
        return self.x + self.w

    @property
    def bottom(self) -> int:
        return self.y + self.h

    @property
    def cx(self) -> int:
        return self.x + self.w // 2

    @property
    def cy(self) -> int:
        return self.y + self.h // 2


boxes = {
    "Interactable": Box(
        "Interactable",
        80,
        55,
        380,
        215,
        ["+CanInteract", "+PromptText", "+BaseInteract(PlayerUI)", "#Interact()", "#PresentInteraction(PlayerUI)"],
    ),
    "InventoryPickup": Box(
        "InventoryPickup",
        560,
        55,
        420,
        215,
        ["-InventoryItemDefinition itemDefinition", "-int amount", "#Interact()"],
    ),
    "Door": Box("Door", 70, 390, 270, 160, ["+Open()", "+Close()", "+Interact()"]),
    "CampingCookingInteractable": Box(
        "CampingCookingInteractable",
        390,
        390,
        390,
        160,
        ["+Interact()", "+EatCookedFood()", "+ReportQuestProgress()"],
    ),
    "MeatPickup": Box("MeatPickup", 835, 390, 260, 160, ["#Interact()"]),
    "TreeLogPickup": Box("TreeLogPickup", 1150, 390, 270, 160, ["#Interact()"]),
    "InventoryItemDefinition": Box(
        "InventoryItemDefinition",
        75,
        705,
        360,
        210,
        ["+ItemId", "+DisplayName", "+Description", "+Icon", "+MaxStack", "+TryUse(...)"],
    ),
    "PlayerInventory": Box(
        "PlayerInventory",
        555,
        700,
        430,
        220,
        [
            "+SlotCount",
            "+Slots",
            "+InventoryChanged",
            "+FeedbackRequested",
            "+ItemAdded",
            "+TryAddItem(...)",
            "+TryConsumeSlot(...)",
            "+TryUseSlot(...)",
        ],
    ),
    "InventoryUIController": Box(
        "InventoryUIController",
        1110,
        715,
        340,
        190,
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
    for line_text in b.lines:
        draw.text((b.x + 18, y), line_text, fill=TEXT, font=SMALL)
        y += 23


def label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int, color=LINE):
    pad = 5
    bbox = draw.textbbox((x, y), text, font=BODY)
    draw.rectangle([bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad], fill=BG)
    draw.text((x, y), text, fill=color, font=BODY)


def polyline(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=LINE, width=2):
    draw.line(pts, fill=color, width=width, joint="curve")


def dashed_polyline(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=DASH, width=2, dash=13, gap=8):
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        dx, dy = x2 - x1, y2 - y1
        length = (dx * dx + dy * dy) ** 0.5
        if length == 0:
            continue
        ux, uy = dx / length, dy / length
        distance = 0.0
        draw_on = True
        while distance < length:
            segment = dash if draw_on else gap
            end = min(distance + segment, length)
            if draw_on:
                xa, ya = x1 + ux * distance, y1 + uy * distance
                xb, yb = x1 + ux * end, y1 + uy * end
                draw.line([(xa, ya), (xb, yb)], fill=color, width=width)
            distance = end
            draw_on = not draw_on


def inheritance_head(draw: ImageDraw.ImageDraw, x: int, y: int):
    pts = [(x, y), (x - 12, y + 22), (x + 12, y + 22)]
    draw.polygon(pts, outline=LINE, fill=BG)
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


def bridge(draw: ImageDraw.ImageDraw, x: int, y: int, radius: int = 9, color=BG):
    # Small white gap around intentional crossings so lines do not visually merge.
    draw.ellipse([x - radius, y - radius, x + radius, y + radius], fill=color)


img = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(img)

for box in boxes.values():
    draw_box(d, box)

# Solid inheritance paths. Each path has its own vertical lane.
inheritance_head(d, boxes["Interactable"].cx, boxes["Interactable"].bottom)
polyline(d, [(205, 390), (205, 320), (boxes["Interactable"].cx, 320), (boxes["Interactable"].cx, boxes["Interactable"].bottom + 22)])
polyline(d, [(585, 390), (585, 335), (boxes["Interactable"].cx + 35, 335), (boxes["Interactable"].cx + 35, boxes["Interactable"].bottom)])
polyline(d, [(boxes["InventoryPickup"].cx, boxes["InventoryPickup"].bottom), (boxes["InventoryPickup"].cx, 320), (boxes["Interactable"].cx - 35, 320), (boxes["Interactable"].cx - 35, boxes["Interactable"].bottom)])

inheritance_head(d, boxes["InventoryPickup"].cx, boxes["InventoryPickup"].bottom)
polyline(d, [(965, 390), (965, 330), (boxes["InventoryPickup"].cx - 35, 330), (boxes["InventoryPickup"].cx - 35, boxes["InventoryPickup"].bottom)])
polyline(d, [(1285, 390), (1285, 345), (boxes["InventoryPickup"].cx + 40, 345), (boxes["InventoryPickup"].cx + 40, boxes["InventoryPickup"].bottom)])

# Dependency: InventoryPickup has one InventoryItemDefinition.
dashed_polyline(d, [(980, 150), (1355, 150), (1355, 650), (435, 650), (435, 810)], DASH)
arrow_head(d, 435, 810, "down", DASH)
label(d, "itemDefinition", 1060, 120, DASH)

# Action/use: InventoryPickup adds item to PlayerInventory.
polyline(d, [(770, 270), (770, 625), (735, 625), (735, 700)], LINE)
arrow_head(d, 735, 700, "down", LINE)
label(d, "thêm vật phẩm", 790, 595, LINE)

# PlayerInventory slot data uses item definition.
dashed_polyline(d, [(555, 815), (435, 815)], DASH)
arrow_head(d, 435, 815, "left", DASH)
label(d, "dữ liệu item trong slot", 270, 780, DASH)

# PlayerInventory updates UI.
dashed_polyline(d, [(985, 810), (1110, 810)], DASH)
arrow_head(d, 1110, 810, "right", DASH)
label(d, "cập nhật UI", 1010, 775, DASH)

# Redraw intentional crossing with tiny white bridge, then restore the top line.
bridge(d, 735, 650)
dashed_polyline(d, [(980, 150), (1355, 150), (1355, 650), (435, 650), (435, 810)], DASH)
arrow_head(d, 435, 810, "down", DASH)

img.save(OUT)
print(OUT)

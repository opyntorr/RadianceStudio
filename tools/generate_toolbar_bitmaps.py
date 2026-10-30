import xml.etree.ElementTree as ET
import uuid
import io
import base64
from PIL import Image, ImageDraw, ImageFont

rui_path = 'src/RadianceStudio.Rhino/RadianceStudioToolbar.rui'
tree = ET.parse(rui_path)
root = tree.getroot()

# List of all macros in order
macros = root.findall('.//macro_item')
print(f"Total macros found: {len(macros)}")

# Identify implemented vs placeholder commands
implemented_commands = {
    '! _RadRingRail',
    '! _RadEuroRail',
    '! _RadCushionRail',
    '! _RadBypassRail',
    '! _RadShank',
    '! _RadKnifeEdgeShank',
    '! _RadCathedralShank',
    '! _RadDShapeShank',
    '! _RadFlatShank',
    '! _RadGemOnCurve',
    '! _RadPave',
    '! _RadProngHead',
    '! _RadBezel',
    '! _RadCutter',
    '! _RadExportSTL',
    '! _RadBOM'
}

num_tools = len(macros)

def draw_tool_icon(size, is_implemented, name):
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Outer rounded rectangle
    margin = 1
    rect = [margin, margin, size - 1 - margin, size - 1 - margin]
    
    if is_implemented:
        # Vibrant Cyan / Blue styling
        bg_color = (18, 38, 58, 240)
        border_color = (0, 180, 240, 255)
        accent_color = (0, 220, 255, 255)
        text_color = (255, 255, 255, 255)
    else:
        # Distinct Muted Amber / Bronze styling for placeholders
        bg_color = (48, 38, 28, 220)
        border_color = (210, 150, 70, 200)
        accent_color = (230, 175, 95, 255)
        text_color = (220, 200, 170, 230)
        
    draw.rounded_rectangle(rect, radius=size//5, fill=bg_color, outline=border_color, width=1)
    
    # Draw simple recognizable vector jewelry geometry
    cx, cy = size / 2.0, size / 2.0
    r = size * 0.28
    
    # Draw characteristic icon
    n_lower = name.lower()
    if 'rail' in n_lower or 'round' in n_lower or 'ring' in n_lower:
        # Ring circle
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], outline=accent_color, width=max(1, size//14))
    elif 'shank' in n_lower or 'band' in n_lower or 'flat' in n_lower:
        # Shank arc with thickness
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], outline=accent_color, width=max(2, size//8))
    elif 'gem' in n_lower or 'diamond' in n_lower or 'stone' in n_lower or 'pave' in n_lower:
        # Diamond gemstone polygon
        pts = [(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)]
        draw.polygon(pts, outline=accent_color, fill=(bg_color[0]+30, bg_color[1]+30, bg_color[2]+30, 200))
    elif 'prong' in n_lower or 'bezel' in n_lower or 'setting' in n_lower or 'halo' in n_lower:
        # Setting prongs (center stone + 4 corner prongs)
        draw.ellipse([cx - r*0.6, cy - r*0.6, cx + r*0.6, cy + r*0.6], outline=accent_color, width=1)
        pr = max(1, size//12)
        for dx, dy in [(-r*0.8, -r*0.8), (r*0.8, -r*0.8), (r*0.8, r*0.8), (-r*0.8, r*0.8)]:
            draw.ellipse([cx+dx-pr, cy+dy-pr, cx+dx+pr, cy+dy+pr], fill=accent_color)
    elif 'cutter' in n_lower:
        # Cutter conical cone
        pts = [(cx - r, cy - r*0.6), (cx + r, cy - r*0.6), (cx, cy + r)]
        draw.polygon(pts, outline=accent_color)
    elif 'stl' in n_lower or 'mesh' in n_lower or 'bom' in n_lower:
        # 3D mesh / box
        draw.rectangle([cx - r, cy - r, cx + r, cy + r], outline=accent_color, width=max(1, size//14))
    else:
        # Generic tool shape
        draw.arc([cx - r, cy - r, cx + r, cy + r], 45, 315, fill=accent_color, width=max(1, size//12))

    # For placeholders: add a small amber badge or "P" in the corner
    if not is_implemented:
        badge_r = max(2, size // 7)
        bx, by = size - margin - badge_r - 2, margin + badge_r + 2
        draw.ellipse([bx - badge_r, by - badge_r, bx + badge_r, by + badge_r], fill=(220, 140, 40, 255), outline=(255, 200, 100, 255))
        
    return img

def create_strip(size):
    strip = Image.new('RGBA', (size, size * num_tools), (0, 0, 0, 0))
    for i, m in enumerate(macros):
        script = m.find('script').text if m.find('script') is not None else ''
        name = m.find('text/locale_1033').text if m.find('text/locale_1033') is not None else ''
        is_impl = script in implemented_commands
        icon = draw_tool_icon(size, is_impl, name)
        strip.paste(icon, (0, i * size))
    
    buf = io.BytesIO()
    strip.save(buf, format='PNG')
    return base64.b64encode(buf.getvalue()).decode('ascii')

small_b64 = create_strip(16)
normal_b64 = create_strip(24)
large_b64 = create_strip(32)

print(f"Generated normal strip: 24x{24*num_tools}, large strip: 32x{32*num_tools}")

# Create or replace <bitmaps> element in RUI
bitmaps_node = root.find('bitmaps')
if bitmaps_node is not None:
    root.remove(bitmaps_node)

bitmaps_node = ET.SubElement(root, 'bitmaps')

# Map each macro to a unique bitmap GUID
bitmap_guids = []
for i, m in enumerate(macros):
    # Deterministic GUID based on index
    bm_guid = f"bb011111-1111-1111-1111-{i+1:012d}"
    bitmap_guids.append(bm_guid)
    m.set('bitmap_id', bm_guid)
    
    script = m.find('script').text if m.find('script') is not None else ''
    is_impl = script in implemented_commands
    
    # Update button_text and tooltip for clear distinction
    btn_text_node = m.find('button_text/locale_1033')
    tooltip_node = m.find('tooltip/locale_1033')
    name_node = m.find('text/locale_1033')
    base_name = name_node.text if name_node is not None else 'Tool'
    
    if btn_text_node is not None:
        raw_text = btn_text_node.text.strip('• ').strip('⏳ ')
        if not is_impl:
            btn_text_node.text = f"•{raw_text}"
        else:
            btn_text_node.text = raw_text

    if tooltip_node is not None:
        raw_tt = tooltip_node.text
        if not is_impl and not raw_tt.startswith('[Roadmap]'):
            tooltip_node.text = f"[Roadmap Preview] {raw_tt}"
        elif is_impl and raw_tt.startswith('[Roadmap]'):
            tooltip_node.text = raw_tt.replace('[Roadmap Preview] ', '').replace('[Roadmap] ', '')

def add_bitmap_elem(tag, item_size, b64_str):
    sub = ET.SubElement(bitmaps_node, tag, {'item_width': str(item_size), 'item_height': str(item_size)})
    for idx, bg in enumerate(bitmap_guids):
        ET.SubElement(sub, 'bitmap_item', {'guid': bg, 'index': str(idx)})
    bm = ET.SubElement(sub, 'bitmap')
    bm.text = b64_str

add_bitmap_elem('small_bitmap', 16, small_b64)
add_bitmap_elem('normal_bitmap', 24, normal_b64)
add_bitmap_elem('large_bitmap', 32, large_b64)

tree.write(rui_path, encoding='utf-8', xml_declaration=True)
print(f"[SUCCESS] Updated {rui_path} with 32x32 & 24x24 icons distinguishing Implemented (Cyan/Gold) vs Placeholders (Amber/Bronze)!")

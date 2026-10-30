import re
import uuid

rui_path = 'src/RadianceStudio.Rhino/RadianceStudioToolbar.rui'
with open(rui_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace mXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX with dXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
fixed_content = re.sub(r'\bm([0-9a-fA-F]{7}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\b', r'd\1', content)

# Validate all GUIDs
guids = re.findall(r'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}', fixed_content)
print(f"Total GUIDs checked: {len(guids)}")
for g in guids:
    uuid.UUID(g)

print("[SUCCESS] All GUIDs are valid hexadecimal UUIDs!")

with open(rui_path, 'w', encoding='utf-8') as f:
    f.write(fixed_content)

print(f"[SUCCESS] Updated {rui_path}")

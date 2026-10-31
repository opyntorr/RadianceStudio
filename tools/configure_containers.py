import os
import xml.etree.ElementTree as ET

containers_path = os.path.expandvars(r'%APPDATA%\McNeel\Rhinoceros\8.0\settings\Scheme__RadianceStudio\containers.xml')
if not os.path.exists(containers_path):
    print(f"[WARN] {containers_path} does not exist yet.")
    exit(0)

# Parse XML
ET.register_namespace('', '')
tree = ET.parse(containers_path)
root = tree.getroot()

rui_file_guid = "c4a754b2-2987-43a1-9b90-1e5f8a09b431"
plugin_guid = "1d4fb0a4-325a-4b6a-b6b1-d1817eb7ff4b"
rui_file_path = os.path.expandvars(r'%APPDATA%\McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0\net8.0\RadianceStudioToolbar.rui')

inspector_guid = "552c1656-787d-4fce-a263-ac0174092e47"
history_guid = "8965008e-eea4-4a86-bfa3-a2d696417531"
layers_guid = "3610bf83-047d-4f7f-93fd-163ea305b493"
layers_plugin_guid = "a6f26df9-1ffd-4cf3-b1ec-a11a28a40892"

right_container_guid = "5a5b89a0-830f-c90d-5f60-35c2e907e18c"
left_container_guid = "4f101111-2222-3333-4444-555555555501"
quick_cmds_guid = "51c420b1-18f9-4672-91de-4a47e7d99001"
jewelry_layers_guid = "51c420b1-18f9-4672-91de-4a47e7d99002"
display_modes_guid = "51c420b1-18f9-4672-91de-4a47e7d99003"
radiance_top_bar_guid = "e8b52194-4360-4963-8a02-984cd4d47150"
standard_toolbars_guid = "171011a9-a956-41ee-853e-3ccc0c0db1d8"

# 1. Register RUI file in <files>
files_node = root.find('.//files')
if files_node is not None:
    found = False
    for fn in files_node.findall('file_name'):
        if fn.get('guid') == rui_file_guid:
            fn.text = rui_file_path
            found = True
            break
    if not found:
        fn = ET.SubElement(files_node, 'file_name', {
            'guid': rui_file_guid,
            'plug_in_guid': plugin_guid,
            'source': 'PlugInFolder'
        })
        fn.text = rui_file_path

# 2. Clean up dock_bars:
dock_bars_node = root.find('.//dock_bars')
to_remove = []
for bar in dock_bars_node.findall('dock_bar'):
    b_guid = bar.get('guid')
    b_name = bar.find('tabs').get('name') if bar.find('tabs') is not None else bar.get('text')
    
    # Remove floating inspector dockbars
    if b_guid == 'bcfdd45a-d869-419e-9348-e7445ddf35f7' or (b_name == 'Jewelry Inspector' and bar.find('placement') is not None and bar.find('placement').get('dock_location') == 'Floating'):
        to_remove.append(bar)
        continue
    
    # Remove duplicate empty dock bars
    if len(bar.attrib) == 1 and len(bar) == 0:
        to_remove.append(bar)
        continue

    # Hide legacy toolbar sidebars
    if b_guid in ['7491ffac-ebf2-4214-bf42-d3d1e4e0f0e1', 'b1c225e5-68ba-44a9-8402-f70a6eb49937', 'd1225ee0-cf16-4f05-b053-60aa46183ac0']:
        p = bar.find('placement')
        if p is not None:
            p.set('visible', 'False')

    # Remove Standard Toolbars dock bar completely
    if b_guid == standard_toolbars_guid or (bar.get('source_group') == radiance_top_bar_guid and b_guid != radiance_top_bar_guid):
        to_remove.append(bar)
        continue

for r in to_remove:
    dock_bars_node.remove(r)

# 3. Create or configure Radiance Studio top dock_bar
top_bar = None
for bar in dock_bars_node.findall('dock_bar'):
    if bar.get('guid') == radiance_top_bar_guid:
        top_bar = bar
        break

if top_bar is None:
    top_bar = ET.SubElement(dock_bars_node, 'dock_bar', {
        'guid': radiance_top_bar_guid,
        'source_group_file': rui_file_guid,
        'source_group': radiance_top_bar_guid
    })

top_bar.set('source_group_file', rui_file_guid)
top_bar.set('source_group', radiance_top_bar_guid)

p = top_bar.find('placement')
if p is None:
    p = ET.SubElement(top_bar, 'placement')
p.set('dock_location', 'Top')
p.set('recent_dock_location', 'Top')
p.set('dock_band_size', '36,54')
p.set('dock_band_item_size', '1,-1')
p.set('docked_placement', '1,0')
p.set('float_point', '215,77')
p.set('float_size', '1000,75')
p.set('visible', 'True')

tabs = top_bar.find('tabs')
if tabs is None:
    tabs = ET.SubElement(top_bar, 'tabs')
tabs.clear()
tabs.set('name', 'Radiance Studio')
tabs.set('selected_item', 'f1011111-1111-1111-1111-111111111101') # Rails
tabs.set('display_style', 'Text')

name_elem = ET.SubElement(tabs, 'name')
loc_elem = ET.SubElement(name_elem, 'locale_1033')
loc_elem.text = 'Radiance Studio'

toolbar_guids = [
    "f1011111-1111-1111-1111-111111111101", # Curve
    "f1011111-1111-1111-1111-111111111102", # Surface
    "f1011111-1111-1111-1111-111111111103", # Solid
    "f1011111-1111-1111-1111-111111111104", # Tools
    "f1011111-1111-1111-1111-111111111105", # Gems
    "f1011111-1111-1111-1111-111111111106", # Settings
    "f1011111-1111-1111-1111-111111111107", # Cutters
    "f1011111-1111-1111-1111-111111111108", # Transform
    "f1011111-1111-1111-1111-111111111109", # Clayoo
    "f1011111-1111-1111-1111-111111111110", # Render
    "f1011111-1111-1111-1111-111111111111", # View
    "f1011111-1111-1111-1111-111111111112", # Mesh
    "f1011111-1111-1111-1111-111111111113", # Dimension
    "f1011111-1111-1111-1111-111111111114", # Analyze
    "f1011111-1111-1111-1111-111111111115", # Misc
    "f1011111-1111-1111-1111-111111111116"  # Manufacturing
]

for tb_guid in toolbar_guids:
    ET.SubElement(tabs, 'tool_bar', {'guid': tb_guid, 'file': rui_file_guid})

# 4. Configure Right Container (Inspector + Parametric History)
for bar in dock_bars_node.findall('dock_bar'):
    if bar.get('guid') == right_container_guid:
        r_tabs = bar.find('tabs')
        if r_tabs is not None:
            r_tabs.set('selected_item', inspector_guid)
            
            # Remove Layers from Right Container so it lives solely on Left
            for p_elem in list(r_tabs.findall('panel')):
                if p_elem.get('guid') == layers_guid:
                    r_tabs.remove(p_elem)

            # Ensure Inspector and History panels are in Right Container
            has_insp = any(p.get('guid') == inspector_guid for p in r_tabs.findall('panel'))
            if not has_insp:
                ET.SubElement(r_tabs, 'panel', {'guid': inspector_guid, 'plug_in': plugin_guid, 'name': 'Jewelry Inspector'})
                
            has_hist = any(p.get('guid') == history_guid for p in r_tabs.findall('panel'))
            if not has_hist:
                ET.SubElement(r_tabs, 'panel', {'guid': history_guid, 'plug_in': plugin_guid, 'name': 'Parametric History'})

# 5. Configure Left Container (Layers)
left_bar = None
for bar in dock_bars_node.findall('dock_bar'):
    if bar.get('guid') == left_container_guid:
        left_bar = bar
        break

if left_bar is None:
    left_bar = ET.SubElement(dock_bars_node, 'dock_bar', {'guid': left_container_guid})

p_left = left_bar.find('placement')
if p_left is None:
    p_left = ET.SubElement(left_bar, 'placement')
p_left.set('dock_location', 'Left')
p_left.set('recent_dock_location', 'Left')
p_left.set('dock_band_size', '280,200')
p_left.set('dock_band_item_size', '-1,1')
p_left.set('docked_placement', '0,0')
p_left.set('float_point', '50,150')
p_left.set('float_size', '280,600')
p_left.set('visible', 'True')

l_tabs = left_bar.find('tabs')
if l_tabs is None:
    l_tabs = ET.SubElement(left_bar, 'tabs')
l_tabs.clear()
l_tabs.set('name', 'Left Container')
left_sidebar_guid = "4f101111-2222-3333-4444-555555555502"
l_tabs.set('selected_item', left_sidebar_guid)
l_tabs.set('display_style', 'BitmapAndText')

name_elem = ET.SubElement(l_tabs, 'name')
loc_elem = ET.SubElement(name_elem, 'locale_1033')
loc_elem.text = 'Workspace'

ET.SubElement(l_tabs, 'panel', {'guid': left_sidebar_guid, 'plug_in': plugin_guid, 'name': 'Workspace'})
ET.SubElement(l_tabs, 'panel', {'guid': layers_guid, 'plug_in': layers_plugin_guid, 'name': 'Layers'})

# 6. Fix last_collection_panel_was_in
last_col = root.find('.//last_collection_panel_was_in')
if last_col is not None:
    for item in last_col.findall('item'):
        if item.get('guid') in [inspector_guid, history_guid]:
            item.set('dock_bar', right_container_guid)
        elif item.get('guid') == layers_guid:
            item.set('dock_bar', left_container_guid)

# 7. Configure Dock Sites (Left: Layers, Right: Inspector/History, Top: Command + Radiance Ribbon)
for site in root.findall('.//dock_site'):
    loc = site.get('location')
    if loc == 'Top':
        bands = site.findall('band')
        for b in bands:
            site.remove(b)
            
        b1 = ET.SubElement(site, 'band')
        ET.SubElement(b1, 'dock_bar', {'guid': '1d3d1785-2332-428b-a838-b2fe39ec50f4'})
        
        b2 = ET.SubElement(site, 'band', {'size': '54'})
        ET.SubElement(b2, 'dock_bar', {'guid': radiance_top_bar_guid})

    elif loc == 'Left':
        for b in site.findall('band'):
            site.remove(b)
        b_left = ET.SubElement(site, 'band', {'size': '280'})
        ET.SubElement(b_left, 'dock_bar', {'guid': left_container_guid})

    elif loc == 'Right':
        bands = site.findall('band')
        for b in bands:
            site.remove(b)
        b_right = ET.SubElement(site, 'band')
        ET.SubElement(b_right, 'dock_bar', {'guid': right_container_guid})

tree.write(containers_path, encoding='utf-8', xml_declaration=True)
print("[SUCCESS] Scheme__RadianceStudio containers.xml configured with Left Layers and Right Inspector/History!")

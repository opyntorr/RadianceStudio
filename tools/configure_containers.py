import os
import xml.etree.ElementTree as ET

def configure_container_file(containers_path):
    if not os.path.exists(containers_path):
        print(f"[WARN] {containers_path} does not exist.")
        return

    # Parse XML
    ET.register_namespace('', '')
    tree = ET.parse(containers_path)
    root = tree.getroot()

    rui_file_guid = "c4a754b2-2987-43a1-9b90-1e5f8a09b431"
    plugin_guid = "1d4fb0a4-325a-4b6a-b6b1-d1817eb7ff4b"
    rui_file_path = os.path.expandvars(r'%APPDATA%\McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0\net8.0\RadianceStudioToolbar.rui')

    radiance_top_bar_guid = "e8b52194-4360-4963-8a02-984cd4d47150"
    cmd_bar_guid = "1d3d1785-2332-428b-a838-b2fe39ec50f4"
    standard_toolbars_guid = "171011a9-a956-41ee-853e-3ccc0c0db1d8"

    # Panel GUIDs
    quick_cmds_guid = "51c420b1-18f9-4672-91de-4a47e7d99001"
    projects_guid = "51c420b1-18f9-4672-91de-4a47e7d99004"
    jewelry_layers_guid = "51c420b1-18f9-4672-91de-4a47e7d99002"
    materials_guid = "51c420b1-18f9-4672-91de-4a47e7d99005"

    history_guid = "8965008e-eea4-4a86-bfa3-a2d696417531"
    inspector_guid = "552c1656-787d-4fce-a263-ac0174092e47"
    properties_guid = "51c420b1-18f9-4672-91de-4a47e7d99006"

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

    # 2. Clean up old / unwanted dock_bars
    dock_bars_node = root.find('.//dock_bars')
    if dock_bars_node is None:
        dock_bars_node = ET.SubElement(root, 'dock_bars')

    unwanted_guids = [
        standard_toolbars_guid,
        'bcfdd45a-d869-419e-9348-e7445ddf35f7',
        '7491ffac-ebf2-4214-bf42-d3d1e4e0f0e1',
        'b1c225e5-68ba-44a9-8402-f70a6eb49937', # Vertical Osnap
        'd1225ee0-cf16-4f05-b053-60aa46183ac0',
        '4f101111-2222-3333-4444-555555555501', # Old combined left container
        '5a5b89a0-830f-c90d-5f60-35c2e907e18c'  # Old tabbed right container
    ]

    to_remove = []
    for bar in dock_bars_node.findall('dock_bar'):
        b_guid = bar.get('guid')
        if b_guid in unwanted_guids:
            to_remove.append(bar)
            continue
        p = bar.find('placement')
        if p is not None and p.get('dock_location') == 'Floating' and b_guid in [quick_cmds_guid, inspector_guid, history_guid, properties_guid]:
            to_remove.append(bar)
            continue
        if len(bar.attrib) <= 1 and len(bar) == 0:
            to_remove.append(bar)

    for r in to_remove:
        dock_bars_node.remove(r)

    # Helper function to get or create a dock_bar
    def get_or_create_dock_bar(bar_guid, name, loc, band_size, item_size, order):
        bar = None
        for b in dock_bars_node.findall('dock_bar'):
            if b.get('guid') == bar_guid:
                bar = b
                break
        if bar is None:
            bar = ET.SubElement(dock_bars_node, 'dock_bar', {'guid': bar_guid})
        
        p = bar.find('placement')
        if p is None:
            p = ET.SubElement(bar, 'placement')
        p.set('dock_location', loc)
        p.set('recent_dock_location', loc)
        p.set('dock_band_size', band_size)
        p.set('dock_band_item_size', item_size)
        p.set('docked_placement', f'0,{order}')
        p.set('float_size', '280,300')
        p.set('visible', 'True')

        t = bar.find('tabs')
        if t is None:
            t = ET.SubElement(bar, 'tabs')
        t.clear()
        t.set('name', name)
        t.set('selected_item', bar_guid)
        t.set('display_style', 'BitmapAndText')

        ne = ET.SubElement(t, 'name')
        ET.SubElement(ne, 'locale_1033').text = name

        ET.SubElement(t, 'panel', {
            'guid': bar_guid,
            'plug_in': plugin_guid,
            'name': name
        })
        return bar

    # 3. Create Left dock_bars: Quick Commands, Projects, Layers, Materials
    get_or_create_dock_bar(quick_cmds_guid, "Quick Commands", "Left", "280,180", "-1,0.25", 0)
    get_or_create_dock_bar(projects_guid, "Projects", "Left", "280,130", "-1,0.18", 1)
    get_or_create_dock_bar(jewelry_layers_guid, "Layers", "Left", "280,260", "-1,0.35", 2)
    get_or_create_dock_bar(materials_guid, "Materials", "Left", "280,150", "-1,0.22", 3)

    # 4. Create Right dock_bars: Dynamic Groups, Dynamic Commands, Properties
    get_or_create_dock_bar(history_guid, "Dynamic Groups", "Right", "340,240", "-1,0.30", 0)
    get_or_create_dock_bar(inspector_guid, "Dynamic Commands", "Right", "340,360", "-1,0.45", 1)
    get_or_create_dock_bar(properties_guid, "Properties", "Right", "340,180", "-1,0.25", 2)

    # 5. Configure Command Prompt dock_bar placement to Bottom
    for bar in dock_bars_node.findall('dock_bar'):
        if bar.get('guid') == cmd_bar_guid:
            p_cmd = bar.find('placement')
            if p_cmd is not None:
                p_cmd.set('dock_location', 'Bottom')
                p_cmd.set('recent_dock_location', 'Bottom')
                p_cmd.set('dock_band_size', '200,28')
                p_cmd.set('dock_band_item_size', '1,-1')
                p_cmd.set('docked_placement', '0,0')
                p_cmd.set('visible', 'True')

    # 6. Configure Radiance Studio top toolbar
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

    p_top = top_bar.find('placement')
    if p_top is None:
        p_top = ET.SubElement(top_bar, 'placement')
    p_top.set('dock_location', 'Top')
    p_top.set('recent_dock_location', 'Top')
    p_top.set('dock_band_size', '36,54')
    p_top.set('dock_band_item_size', '1,-1')
    p_top.set('docked_placement', '1,0')
    p_top.set('float_point', '215,77')
    p_top.set('float_size', '1000,75')
    p_top.set('visible', 'True')

    tabs = top_bar.find('tabs')
    if tabs is None:
        tabs = ET.SubElement(top_bar, 'tabs')
    tabs.clear()
    tabs.set('name', 'Radiance Studio')
    tabs.set('selected_item', 'f1011111-1111-1111-1111-111111111101') # Curve
    tabs.set('display_style', 'Text') # Text labels for the top tabs ("Curve", "Surface", etc.)

    name_elem = ET.SubElement(tabs, 'name')
    ET.SubElement(name_elem, 'locale_1033').text = 'Radiance Studio'

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

    # 7. Configure Dock Sites
    for site in root.findall('.//dock_site'):
        loc = site.get('location')
        if loc == 'Top':
            for b in site.findall('band'):
                site.remove(b)
            b_top = ET.SubElement(site, 'band', {'size': '54'})
            ET.SubElement(b_top, 'dock_bar', {'guid': radiance_top_bar_guid})

        elif loc == 'Bottom':
            for b in site.findall('band'):
                site.remove(b)
            b_bot = ET.SubElement(site, 'band', {'size': '28'})
            ET.SubElement(b_bot, 'dock_bar', {'guid': cmd_bar_guid})

        elif loc == 'Left':
            for b in site.findall('band'):
                site.remove(b)
            b_left = ET.SubElement(site, 'band', {'size': '280'})
            ET.SubElement(b_left, 'dock_bar', {'guid': quick_cmds_guid, 'size': '0.25'})
            ET.SubElement(b_left, 'dock_bar', {'guid': projects_guid, 'size': '0.18'})
            ET.SubElement(b_left, 'dock_bar', {'guid': jewelry_layers_guid, 'size': '0.35'})
            ET.SubElement(b_left, 'dock_bar', {'guid': materials_guid, 'size': '0.22'})

        elif loc == 'Right':
            for b in site.findall('band'):
                site.remove(b)
            b_right = ET.SubElement(site, 'band', {'size': '340'})
            ET.SubElement(b_right, 'dock_bar', {'guid': history_guid, 'size': '0.30'})
            ET.SubElement(b_right, 'dock_bar', {'guid': inspector_guid, 'size': '0.45'})
            ET.SubElement(b_right, 'dock_bar', {'guid': properties_guid, 'size': '0.25'})

    # 8. Configure <last_collection_panel_was_in>
    last_col = root.find('.//last_collection_panel_was_in')
    if last_col is None:
        last_col = ET.SubElement(root, 'last_collection_panel_was_in')

    our_panel_map = {
        quick_cmds_guid: quick_cmds_guid,
        projects_guid: projects_guid,
        jewelry_layers_guid: jewelry_layers_guid,
        materials_guid: materials_guid,
        history_guid: history_guid,
        inspector_guid: inspector_guid,
        properties_guid: properties_guid
    }

    # Remove conflicting entries (like native layers in left container)
    for it in list(last_col.findall('item')):
        g = it.get('guid')
        if g in our_panel_map:
            last_col.remove(it)
        elif g == '3610bf83-047d-4f7f-93fd-163ea305b493': # Native layers
            last_col.remove(it)

    for p_guid, db_guid in our_panel_map.items():
        ET.SubElement(last_col, 'item', {'guid': p_guid, 'dock_bar': db_guid})

    tree.write(containers_path, encoding='utf-8', xml_declaration=True)
    print(f"[SUCCESS] Configured {containers_path} with 4 Left panels, 3 Right panels, and Top Bitmap toolbar!")

def configure_containers():
    # STRICT SCHEME ISOLATION: NEVER touch Scheme__Default
    targets = [
        os.path.expandvars(r'%APPDATA%\McNeel\Rhinoceros\8.0\settings\Scheme__RadianceStudio\containers.xml')
    ]
    for p in targets:
        configure_container_file(p)

    # Also configure scheme settings (dark mode, obsidian viewport, jewelry theme)
    try:
        import configure_scheme_settings
        configure_scheme_settings.configure_scheme_settings()
    except Exception as ex:
        print(f"[WARN] Failed to configure scheme settings: {ex}")

if __name__ == '__main__':
    configure_containers()

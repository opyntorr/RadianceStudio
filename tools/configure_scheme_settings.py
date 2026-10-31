import os
import xml.etree.ElementTree as ET

def configure_scheme_settings():
    settings_path = os.path.expandvars(r'%APPDATA%\McNeel\Rhinoceros\8.0\settings\settings-Scheme__RadianceStudio.xml')
    if not os.path.exists(settings_path):
        print(f"[INFO] Creating {settings_path}")
        root = ET.Element('settings', {'id': '2.0'})
        tree = ET.ElementTree(root)
        s_node = ET.SubElement(root, 'settings')
    else:
        tree = ET.parse(settings_path)
        root = tree.getroot()
        s_node = root.find('settings')
        if s_node is None:
            s_node = ET.SubElement(root, 'settings')

    options = s_node.find('child[@key="Options"]')
    if options is None:
        options = ET.SubElement(s_node, 'child', {'key': 'Options'})

    # 1. Advanced -> DarkMode = True
    adv = options.find('child[@key="Advanced"]')
    if adv is None:
        adv = ET.SubElement(options, 'child', {'key': 'Advanced'})
    dm = adv.find('entry[@key="DarkMode"]')
    if dm is None:
        dm = ET.SubElement(adv, 'entry', {'key': 'DarkMode'})
    dm.text = 'True'

    # 2. Appearance
    app = options.find('child[@key="Appearance"]')
    if app is None:
        app = ET.SubElement(options, 'child', {'key': 'Appearance'})

    app_entries = {
        'ViewportBackgroundColor': '255,20,20,22',
        'FrameBackgroundColor': '255,28,28,32',
        'GridThinLineColor': '255,32,32,36',
        'GridThickLineColor': '255,48,48,54',
        'GridXAxisLineColor': '255,200,50,50',
        'GridYAxisLineColor': '255,50,200,50',
        'GridZAxisLineColor': '255,50,150,255',
        'SelectedObjectColor': '255,255,215,0',
        'LockedObjectColor': '255,70,70,75',
        'FeedbackColor': '255,0,229,255',
        'CommandPromptBackgroundColor': '255,24,24,26',
        'CommandPromptTextColor': '255,240,240,240',
        'CommandPromptActiveHypertextColor': '255,212,175,55',
        'CommandPromptHypertextColor': '255,212,175,55',
        'CommandPromptFontHeight': '110',
        'UsePaintColors': 'True',
        'MenuVisible': 'False',
        'CommandPromptPosition': 'Bottom',
        'ShowOsnapBar': 'True',
        'ShowStatusBar': 'True',
        'ShowSideBar': 'False',
        'ShowViewportTitles': 'True',
        'ShowTitleBar': 'True'
    }

    for k, v in app_entries.items():
        e = app.find(f'entry[@key="{k}"]')
        if e is None:
            e = ET.SubElement(app, 'entry', {'key': k})
        e.text = v

    # 3. UiPaintColors
    uipc = options.find('child[@key="UiPaintColors"]')
    if uipc is None:
        uipc = ET.SubElement(options, 'child', {'key': 'UiPaintColors'})

    uipc_entries = {
        'NormalStart': '255,30,30,34',
        'NormalEnd': '255,24,24,28',
        'NormalBorder': '255,45,45,50',
        'MouseOverControlStart': '255,45,45,52',
        'MouseOverControlEnd': '255,50,50,58',
        'MouseOverControlBorder': '255,212,175,55',
        'HotStart': '255,40,40,46',
        'HotEnd': '255,40,40,46',
        'PressedStart': '255,20,20,24',
        'PressedEnd': '255,212,175,55',
        'TextEnabled': '255,240,240,240',
        'TextDisabled': '255,100,100,105',
        'ActiveViewportTitle': '255,212,175,55',
        'InactiveViewportTitle': '255,140,140,145',
        'ActiveCaption': '255,240,240,240',
        'InactiveCaption': '255,140,140,145',
        'InactiveTabBackground': '255,20,20,22'
    }

    for k, v in uipc_entries.items():
        e = uipc.find(f'entry[@key="{k}"]')
        if e is None:
            e = ET.SubElement(uipc, 'entry', {'key': k})
        e.text = v

    tree.write(settings_path, encoding='utf-8', xml_declaration=True)
    print(f"[SUCCESS] Configured {settings_path} for Obsidian Dark Jewelry theme!")

if __name__ == '__main__':
    configure_scheme_settings()

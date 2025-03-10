import os
import sys
import traceback
import zipfile
import xml.etree.ElementTree as ET

MODS_DIR = "mods"
RELEASE_DIR = "releases"
ZIPMODS_DIR = "zipmods"
TERMS_FILE = "README - OpenNSFW SFX Pack Terms.pdf"
README_FILE = "README.md"


def parse_manifest(manifest_path):
    tree = ET.parse(manifest_path)
    root = tree.getroot()
    name = root.find('name').text
    version = root.find('version').text
    author = root.find('author').text
    return name, version, author


def create_release_zip(folder_path, zipmod_path, release_version, name, version):
    release_filename = f"Release.v{release_version}.{name}.v{version}.zip"
    release_path = os.path.join(RELEASE_DIR, release_filename)
    with zipfile.ZipFile(release_path, 'w', zipfile.ZIP_STORED) as zipf:
        # Add the zipmod file
        zipf.write(zipmod_path, os.path.basename(zipmod_path))

        # Add the contents of the credits folder if it exists in the mod folder
        credits_folder_path = os.path.join(folder_path, "credits")
        if os.path.exists(credits_folder_path):
            for root, dirs, files in os.walk(credits_folder_path):
                for file in files:
                    file_path = os.path.join(root, file)
                    zipf.write(file_path, file)
        
        # Add TERMS file
        if os.path.exists(TERMS_FILE):
            zipf.write(TERMS_FILE, os.path.basename(TERMS_FILE))


def main():
    release_version = None
    if len(sys.argv) < 1:
        print("Error: Release version not specified.")
        sys.exit(1)
    
    release_version = sys.argv[1]
    if not os.path.exists(RELEASE_DIR):
        os.makedirs(RELEASE_DIR)

    # clear files in the RELEASE_DIR folder
    if not os.path.exists(RELEASE_DIR):
        os.makedirs(RELEASE_DIR)
    for file in os.listdir(RELEASE_DIR):
        file_path = os.path.join(RELEASE_DIR, file)
        os.remove(file_path)

    for folder_name in os.listdir(MODS_DIR):
        folder_path = os.path.join(MODS_DIR, folder_name)
        manifest_path = os.path.join(folder_path, "manifest.xml")
        if os.path.exists(manifest_path):
            name, version, author = parse_manifest(manifest_path)
            output_filename = f"[{author}] {name} v{version}.zipmod"
            output_path = os.path.join(ZIPMODS_DIR, output_filename)
            create_release_zip(folder_path, output_path, release_version, name, version)
        else:
            print(f"Manifest file does not exist: {manifest_path}")


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        traceback.print_exc()
        input("Press Enter...")
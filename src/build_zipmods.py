import os
import sys
import traceback
import zipfile
import xml.etree.ElementTree as ET

MODS_DIR = "mods"
OUTPUT_DIR = "zipmods"
RELEASE_DIR = "releases"
MIN_FILE_SIZE_KB = 20
TERMS_FILE = "README - OpenNSFW SFX Pack Terms.pdf"
README_FILE = "README.md"

def parse_manifest(manifest_path):
    tree = ET.parse(manifest_path)
    root = tree.getroot()
    name = root.find('name').text
    version = root.find('version').text
    author = root.find('author').text
    return name, version, author

def zip_folder(folder_path, output_path):
    with zipfile.ZipFile(output_path, 'w', zipfile.ZIP_STORED) as zipf:
        # Add manifest.xml
        manifest_path = os.path.join(folder_path, "manifest.xml")
        if os.path.exists(manifest_path):
            zipf.write(manifest_path, "manifest.xml")
        
        # Add abdata folder
        abdata_path = os.path.join(folder_path, "abdata")
        for root, dirs, files in os.walk(abdata_path):
            for file in files:
                file_path = os.path.join(root, file)
                arcname = os.path.relpath(file_path, folder_path)
                zipf.write(file_path, arcname)

def assert_unity3d_files_not_empty(folder_path):
    for root, dirs, files in os.walk(folder_path):
        for file in files:
            if file.endswith(".unity3d"):
                file_path = os.path.join(root, file)
                if os.path.getsize(file_path) < MIN_FILE_SIZE_KB * 1024:
                    raise Exception(f"{file_path} is smaller than 20KB")

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
                    arcname = os.path.relpath(file_path, folder_path)
                    zipf.write(file_path, arcname)
        
        # Add other files and folders excluding manifest.xml and abdata
        for root, dirs, files in os.walk(folder_path):
            for file in files:
                if file == "manifest.xml" or root.startswith(os.path.join(folder_path, "abdata")):
                    continue
                file_path = os.path.join(root, file)
                arcname = os.path.relpath(file_path, folder_path)
                zipf.write(file_path, arcname)
        
        # Add TERMS file
        if os.path.exists(TERMS_FILE):
            zipf.write(TERMS_FILE, os.path.basename(TERMS_FILE))

def find_abdata_path(folder_path):
    abdata_studio_path = os.path.join(folder_path, "abdata/studio")
    if os.path.exists(abdata_studio_path):
        for item in os.listdir(abdata_studio_path):
            item_path = os.path.join(abdata_studio_path, item)
            if os.path.isdir(item_path) and item.lower() != "info":
                return item_path
    return None

def main():
    release_version = None
    if len(sys.argv) > 1 and sys.argv[1] == "--release":
        if len(sys.argv) < 3:
            print("Error: Release version not specified.")
            sys.exit(1)
        release_version = sys.argv[2]
        if not os.path.exists(RELEASE_DIR):
            os.makedirs(RELEASE_DIR)

    # Clear the OUTPUT_DIR folder
    if os.path.exists(OUTPUT_DIR):
        for file in os.listdir(OUTPUT_DIR):
            file_path = os.path.join(OUTPUT_DIR, file)
            os.remove(file_path)
    else:
        os.makedirs(OUTPUT_DIR)

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
            output_path = os.path.join(OUTPUT_DIR, output_filename)
            abdata_path = find_abdata_path(folder_path)
            if abdata_path:
                assert_unity3d_files_not_empty(abdata_path)
                print(f"Zipping folder: {folder_path} -> {output_path}")
                zip_folder(folder_path, output_path)
                if release_version:
                    create_release_zip(folder_path, output_path, release_version, name, version)
            else:
                print(f"abdata/studio/open_nsfw folder does not exist: {abdata_path}")
        else:
            print(f"Manifest file does not exist: {manifest_path}")

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        traceback.print_exc()
        input("Press Enter...")

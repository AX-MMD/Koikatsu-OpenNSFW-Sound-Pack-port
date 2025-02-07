import os
import sys
import traceback

def process_folder(folder_path):
    for filename in os.listdir(folder_path):
        if filename.endswith(".wav"):
            new_path = os.path.join(folder_path, filename.replace(' ', '_').replace('-', '_'))
            os.rename(os.path.join(folder_path, filename), new_path)
            print(os.path.splitext(os.path.basename(new_path))[0])


if __name__ == "__main__":
    try:
        if len(sys.argv) != 2:
            print("Usage: drag and drop a .wav file or a folder onto the script")
            input("Press Enter...")
            sys.exit(1)

        path = sys.argv[1]
    
        if os.path.isdir(path):
            process_folder(path)
        elif os.path.isfile(path) and path.endswith(".wav"):
            new_path = os.path.join(os.path.dirname(path), os.path.basename(path).replace(' ', '_').replace('-', '_'))
            os.rename(
                os.path.join(os.path.dirname(path), os.path.basename(path)), 
                new_path, 
            )
            print(os.path.splitext(os.path.basename(new_path))[0])
        else:
            print("The provided path is not a valid .wav file or directory")
            input("Press Enter...")
            sys.exit(1)
    except Exception as e:
        traceback.print_exc()
    finally:
        input("Press Enter...")
    
import os
import sys
from pydub import AudioSegment
import traceback

# The silence before the start of the audio must be at least 50ms to avoid the
# occasionnal audio volume spike at the beginning of the audio in Koikatsu Studio.

# I use this script to increase the silence duration by whatever is needed for
# it to be 50ms or more.

TARGET_DURATION_SECONDS = None
SILENCE_DURATION_SECONDS = 0.03

def extend_wav_file(file_path, target_duration_ms, silence_duration_ms):
    audio = AudioSegment.from_wav(file_path)
    if silence_duration_ms > 0:
        silence = AudioSegment.silent(duration=silence_duration_ms)
        audio = silence + audio

    if target_duration_ms is None:
        extended_audio = audio
    elif len(audio) < target_duration_ms:
        silence = AudioSegment.silent(duration=target_duration_ms - len(audio))
        extended_audio = audio + silence
    else:
        extended_audio = audio[:target_duration_ms]

    extended_audio.export(file_path, format="wav")
    print(f"Processed {file_path}")

def process_folder(folder_path, target_duration_ms, silence_duration_ms):
    for filename in os.listdir(folder_path):
        if filename.endswith(".wav"):
            file_path = os.path.join(folder_path, filename)
            extend_wav_file(file_path, target_duration_ms, silence_duration_ms)

if __name__ == "__main__":
    try:
        if len(sys.argv) != 2:
            print("Usage: drag and drop a .wav file or a folder onto the script")
            input("Press Enter...")
            sys.exit(1)

        path = sys.argv[1]
        target_duration_seconds = TARGET_DURATION_SECONDS
        silence_duration_seconds = SILENCE_DURATION_SECONDS

        target_duration_ms = int(target_duration_seconds * 1000) if target_duration_seconds else None
        silence_duration_ms = int(silence_duration_seconds * 1000)

    
        if os.path.isdir(path):
            process_folder(path, target_duration_ms, silence_duration_ms)
        elif os.path.isfile(path) and path.endswith(".wav"):
            extend_wav_file(path, target_duration_ms, silence_duration_ms)
        else:
            print("The provided path is not a valid .wav file or directory")
            input("Press Enter...")
            sys.exit(1)
    except Exception as e:
        traceback.print_exc()
    finally:
        input("Press Enter...")
    
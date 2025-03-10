@echo off

SET project_path=./

IF /I "%1"==".DEFAULT_GOAL" GOTO .DEFAULT_GOAL 
IF /I "%1"=="build" GOTO build
IF /I "%1"=="release" GOTO release
GOTO error

:.DEFAULT_GOAL 
    CALL make.bat build
    GOTO :EOF

:build
    echo "Building zipmods..."
    python %project_path%src/build_zipmods.py
    GOTO :EOF

:release
    IF "%2"=="" (
        ECHO Error: Release version not specified.
        GOTO :EOF
    )
    SET release_version=%2
    echo "Building release zips for version %release_version%..."
    python %project_path%src/build_release.py %release_version%
    GOTO :EOF

:error
    IF "%1"=="" (
        ECHO make: *** No targets specified and no makefile found.  Stop.
    ) ELSE (
        ECHO make: *** No rule to make target '%1%'. Stop.
    )
    GOTO :EOF
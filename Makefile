# Unity Project Makefile

# Variables
UNITY_VERSION ?= 6000.1.5f1
UNITY_PATH ?= /Applications/Unity/Hub/Editor/$(UNITY_VERSION)/Unity.app/Contents/MacOS/Unity
PROJECT_PATH := $(shell pwd)/src/dArtagnan.Unity
BUILD_PATH := $(shell pwd)/builds
LOG_PATH := $(shell pwd)/logs

# Create necessary directories
$(shell mkdir -p $(BUILD_PATH) $(LOG_PATH))

# Default target
.PHONY: all
all: help

# Help command
.PHONY: help
help:
	@echo "Available commands:"
	@echo "  make run              - Run the Unity project in editor mode"
	@echo "  make build            - Build the project for all platforms"
	@echo "  make build-mac        - Build the project for macOS"
	@echo "  make build-win        - Build the project for Windows"
	@echo "  make build-linux      - Build the project for Linux"
	@echo "  make clean            - Clean build artifacts"
	@echo "  make test             - Run Unity tests"
	@echo "  make update           - Update Unity packages"

# Run Unity in editor mode
.PHONY: run
run:
	@echo "Starting Unity Editor..."
	"$(UNITY_PATH)" -projectPath "$(PROJECT_PATH)" -logFile "$(LOG_PATH)/editor.log"

# Build for all platforms
.PHONY: build
build: build-mac build-win build-linux

# Build for macOS
.PHONY: build-mac
build-mac:
	@echo "Building for macOS..."
	"$(UNITY_PATH)" -projectPath "$(PROJECT_PATH)" \
		-executeMethod BuildScript.BuildMac \
		-logFile "$(LOG_PATH)/build-mac.log" \
		-quit -batchmode -nographics

# Build for Windows
.PHONY: build-win
build-win:
	@echo "Building for Windows..."
	"$(UNITY_PATH)" -projectPath "$(PROJECT_PATH)" \
		-executeMethod BuildScript.BuildWindows \
		-logFile "$(LOG_PATH)/build-win.log" \
		-quit -batchmode -nographics

# Build for Linux
.PHONY: build-linux
build-linux:
	@echo "Building for Linux..."
	"$(UNITY_PATH)" -projectPath "$(PROJECT_PATH)" \
		-executeMethod BuildScript.BuildLinux \
		-logFile "$(LOG_PATH)/build-linux.log" \
		-quit -batchmode -nographics

# Clean build artifacts
.PHONY: clean
clean:
	@echo "Cleaning build artifacts..."
	rm -rf "$(BUILD_PATH)"/*
	rm -rf "$(LOG_PATH)"/*

# Run Unity tests
.PHONY: test
test:
	@echo "Running Unity tests..."
	"$(UNITY_PATH)" -projectPath "$(PROJECT_PATH)" \
		-runTests \
		-testPlatform PlayMode \
		-logFile "$(LOG_PATH)/tests.log" \
		-quit -batchmode -nographics

# Update Unity packages
.PHONY: update
update:
	@echo "Updating Unity packages..."
	"$(UNITY_PATH)" -projectPath "$(PROJECT_PATH)" \
		-executeMethod PackageManager.UpdatePackages \
		-logFile "$(LOG_PATH)/update.log" \
		-quit -batchmode -nographics

# Server build and run commands
.PHONY: server-build
server-build:
	@echo "Building server..."
	dotnet build src/dArtagnan.Server/dArtagnan.Server.csproj

.PHONY: server-run
server-run:
	@echo "Running server..."
	dotnet run --project src/dArtagnan.Server/dArtagnan.Server.csproj

# Development commands
.PHONY: dev
dev: server-build server-run

# Clean all
.PHONY: clean-all
clean-all: clean
	@echo "Cleaning all project artifacts..."
	rm -rf "$(PROJECT_PATH)/Library"
	rm -rf "$(PROJECT_PATH)/Temp"
	rm -rf "$(PROJECT_PATH)/Logs"
	rm -rf "$(PROJECT_PATH)/obj"
	rm -rf "$(PROJECT_PATH)/bin" 
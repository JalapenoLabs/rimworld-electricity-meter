
all: build

# Builds the mod using dotnet
# Read the README.md for more info
build:
	dotnet build .vscode

publish: build
	@echo "Running PowerShell publish script…"
	powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(CURDIR)/publish.ps1"

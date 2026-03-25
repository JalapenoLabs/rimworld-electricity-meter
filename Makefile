
all: build

# Builds the mod DLL using dotnet.
# RimworldForCICD DLLs are provided by the root-level submodule.
build:
	dotnet build mod.csproj

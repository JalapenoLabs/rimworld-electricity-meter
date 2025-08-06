<#
.SYNOPSIS
    Build and package workshop files for Steam Workshop
#>

# force ASCII-only output
[Console]::OutputEncoding = [System.Text.Encoding]::ASCII

Write-Output "Building workshop files..."

# Remove any old workshop folder so we start fresh
if (Test-Path "workshop") {
    Write-Output "Removing existing 'workshop' directory..."
    Remove-Item -Recurse -Force "workshop"
}

# Create a clean workshop directory
Write-Output "Creating 'workshop' directory..."
New-Item -ItemType Directory -Name "workshop" | Out-Null

# Copy necessary files and folders into the workshop directory
Write-Output "Copying files..."
Copy-Item -Recurse -Path "About" -Destination "workshop"
Copy-Item -Recurse -Path "Textures" -Destination "workshop"
Copy-Item -Recurse -Path "1.6" -Destination "workshop"
Copy-Item -Path "README.md", "LICENSE" -Destination "workshop"

# Produce version string, e.g. 2025.8.5
$VERSION = Get-Date -Format 'yyyy.M.d'
Write-Output "Updating About/About.xml -> modVersion = $VERSION"

# Replace whatever is between the <modVersion> tags
(Get-Content "About/About.xml") `
    -replace '<modVersion>[^<]+</modVersion>', "<modVersion>$VERSION</modVersion>" `
| Set-Content "About/About.xml"

Write-Output "========================================="
Write-Output "Updated modVersion in About/About.xml to:"
Select-String -Pattern 'modVersion' -Path "About/About.xml"

# Prompt the user for the changenote
$ChangeNote = Read-Host "Enter changenote"

# Build and escape the workshop path for VDF
$workshopPath = "$((Get-Location).Path)\workshop"
# double each backslash
$escapedPath = $workshopPath.Replace('\', '/')

# Create the workshop_build.vdf file
@"
"workshopitem"
{
    "appid"           "294100"   # Rimworld game ID
    "publishedfileid" "3542488017"
    "contentfolder"   "$escapedPath"
    "changenote"      "$ChangeNote"
}
"@ | Set-Content -Path "workshop_build.vdf"

# Display the generated VDF
Write-Output "Contents of workshop_build.vdf:"
Get-Content "workshop_build.vdf"

$SteamUsername = Read-Host "Enter Steam Username"
$SecurePassword = Read-Host "Enter Steam Password" -AsSecureString

# Convert SecureString to plain text in memory only
$BSTR = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
$SteamPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($BSTR)

# Compute the absolute path to workshop_build.vdf
#    - Resolve-Path gives you the full filesystem path
#    - .ProviderPath strips off the PSDrive prefix if any
$vdfPath = (Resolve-Path .\workshop_build.vdf).ProviderPath

Write-Output "Using VDF at: $vdfPath"

# Upload via steamcmd
& steamcmd +login $SteamUsername $SteamPassword +workshop_build_item $vdfPath +quit

[CmdletBinding()]
param(
    [switch] $AllowMissingLanguages,
    [switch] $AllowEnglishFallbacks
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$catalogRoot = Join-Path $root 'src\Registry.App\Resources\locales'
$appSourceRoot = Join-Path $root 'src\Registry.App'
$installerPath = Join-Path $root 'installer\Registry.iss'

# Release contract: keep this list aligned with LocalizationService and the
# installer language map. Missing locales are intentional release failures.
$supportedLanguages = @(
    'en', 'hu', 'es', 'de', 'fr', 'it', 'pl', 'pt-BR', 'pt-PT', 'tr', 'id', 'ro', 'cs', 'sk',
    'ru', 'uk', 'zh-CN', 'zh-TW', 'ar', 'vi', 'th', 'hi', 'ja', 'ko', 'nl', 'el', 'bg', 'hr',
    'sr', 'sl', 'sv', 'da', 'fi', 'nb'
)

function Read-Catalog([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json -AsHashtable
}

function Get-Placeholders([string] $Value) {
    @([regex]::Matches($Value, '\{[^}]+\}') | ForEach-Object Value | Sort-Object)
}

$sameAsEnglishIsTechnical = @(
    'action.ok',
    'choice.mica',
    'editor.hex',
    'editor.hexadecimal',
    'status.search-match-summary',
    'status.value-summary',
    'label.name',
    'label.data',
    'editor.base'
)

$errors = [System.Collections.Generic.List[string]]::new()
$english = Read-Catalog (Join-Path $catalogRoot 'en\windows.json')
if ($null -eq $english) { throw 'Missing canonical English catalog.' }
$expectedKeys = @($english.Keys | Sort-Object)

foreach ($language in $supportedLanguages) {
    $path = Join-Path $catalogRoot "$language\windows.json"
    try {
        $catalog = Read-Catalog $path
    } catch {
        $errors.Add("Invalid JSON app catalog: $language/windows.json ($($_.Exception.Message))")
        continue
    }
    if ($null -eq $catalog) {
        if (-not $AllowMissingLanguages) { $errors.Add("Missing app catalog: $language/windows.json") }
        continue
    }

    $actualKeys = @($catalog.Keys | Sort-Object)
    $missing = @($expectedKeys | Where-Object { $_ -notin $actualKeys })
    $extra = @($actualKeys | Where-Object { $_ -notin $expectedKeys })
    if ($missing.Count) { $errors.Add("$language missing keys: $($missing -join ', ')") }
    if ($extra.Count) { $errors.Add("$language has unknown keys: $($extra -join ', ')") }

    foreach ($key in $expectedKeys) {
        if (-not $catalog.ContainsKey($key)) { continue }
        $value = [string]$catalog[$key]
        if ([string]::IsNullOrWhiteSpace($value)) { $errors.Add("$language empty value: $key"); continue }
        if ($value -match '[\x00-\x08\x0B\x0C\x0E-\x1F]') {
            $errors.Add("$language contains control characters: $key")
        }
        $englishPlaceholders = (Get-Placeholders ([string]$english[$key])) -join '|'
        $localizedPlaceholders = (Get-Placeholders $value) -join '|'
        if ($englishPlaceholders -ne $localizedPlaceholders) { $errors.Add("$language placeholder mismatch: $key") }
        $technical = $key -match '^(app\.title|choice\.|label\.|action\.(monitor|export|import|permissions)|.*\.description)$'
        if ($language -ne 'en' -and -not $AllowEnglishFallbacks -and $value -ceq ([string]$english[$key]) -and -not $technical -and $key -notin $sameAsEnglishIsTechnical) {
            $errors.Add("$language still English for review-required key: $key")
        }
    }
}

# Every user-facing XAML literal must have a semantic Tag or automation key.
# Bindings and registry data-type names are data contracts, not UI copy.
foreach ($file in (Get-ChildItem -LiteralPath $appSourceRoot -Recurse -File -Filter *.xaml | Where-Object FullName -notmatch '\\(bin|obj)\\')) {
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        if ($line -match '(Text|Content|Header|PlaceholderText|ToolTipService\.ToolTip)="([^"]+)"' -and
            $line -notmatch 'x:Bind|Tag="|AutomationProperties\.Name="|AutomationProperties\.HelpText="|FontIcon|Glyph=' -and
            $line -notmatch '^\s*Title="Registry"' -and
            $line -notmatch 'Content="(String|ExpandString|DWord|QWord|Binary|MultiString|32-bit view|64-bit view)"') {
            $errors.Add("Unkeyed XAML copy: $($file.FullName):$lineNumber")
        }
    }
}

# Semantic keys referenced from XAML and code must exist in the canonical
# catalog. This catches a control that looks keyed but can never localize.
$referencedKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($file in (Get-ChildItem -LiteralPath $appSourceRoot -Recurse -File | Where-Object FullName -notmatch '\\(bin|obj)\\')) {
    $source = Get-Content -Raw -LiteralPath $file.FullName
    $patterns = if ($file.Extension -eq '.xaml') {
        @('(?:Tag|AutomationProperties\.Name|AutomationProperties\.HelpText)="([a-z][a-z0-9.-]+)"')
    } else {
        @('LocalizationService\.(?:Get|Format)\("([a-z][a-z0-9.-]+)"')
    }
    foreach ($pattern in $patterns) {
        foreach ($match in [regex]::Matches($source, $pattern)) {
            $key = $match.Groups[1].Value
            if ($key.Contains('.')) { [void]$referencedKeys.Add($key) }
        }
    }
}
foreach ($key in $referencedKeys) {
    if (-not $english.ContainsKey($key)) { $errors.Add("Source references missing app catalog key: $key") }
}

# Dynamic user-facing C# copy must route through LocalizationService or carry
# an explicit i18n-ignore comment for a registry/protocol literal.
$codeFiles = Get-ChildItem -LiteralPath $appSourceRoot -Recurse -File -Filter *.cs |
    Where-Object FullName -notmatch '\\(bin|obj)\\' |
    Where-Object FullName -notmatch '\\Services\\LocalizationService\.cs$'
foreach ($file in $codeFiles) {
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        # ShowStatus translates its title through LocalizationService.StatusTitle;
        # inspect the message argument rather than treating its internal status
        # identifier as display copy.
        if ($line -match 'ShowStatus\([^,]+,\s*\$?"|CreateDialog\(\s*"|UpdateStatus\(\s*\$?"|Title\s*=\s*"|Message\s*=\s*"|PrimaryButtonText\s*=\s*"|CloseButtonText\s*=\s*"' -and
            $line -notmatch 'ShowStatus\(LocalizationService\.' -and
            $line -notmatch 'i18n-ignore') {
            $errors.Add("Unlocalized C# UI copy: $($file.FullName):$lineNumber")
        }
    }
}

$installerText = Get-Content -Raw -LiteralPath $installerPath
$installerLanguages = @([regex]::Matches($installerText, 'Name:\s*"([^"]+)";\s*MessagesFile:') | ForEach-Object { $_.Groups[1].Value })
if ($installerLanguages.Count -lt 27) { $errors.Add("Inno language set unexpectedly small: $($installerLanguages.Count)") }

Write-Host "Canonical app keys: $($expectedKeys.Count)"
Write-Host "Required app locales: $($supportedLanguages.Count); Inno locales: $($installerLanguages.Count)"
if ($errors.Count) {
    $errors | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
    throw "Localization check failed with $($errors.Count) issue(s)."
}
Write-Host 'Localization check passed: catalogs, source copy, C# UI copy, and installer coverage are consistent.'

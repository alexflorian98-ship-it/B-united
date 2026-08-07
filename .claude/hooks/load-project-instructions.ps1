[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Get-Sha256 {
    param([Parameter(Mandatory = $true)] [string] $Value)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($algorithm.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
    }
}

try {
    $stdin = [Console]::OpenStandardInput()
    $reader = [System.IO.StreamReader]::new($stdin, [System.Text.UTF8Encoding]::new($false))
    try {
        $inputText = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
    if ([string]::IsNullOrWhiteSpace($inputText)) {
        [Console]::Error.WriteLine('Missing UserPromptSubmit hook input.')
        exit 2
    }

    $hookInput = $inputText | ConvertFrom-Json
    $projectRoot = if ($null -ne $hookInput.PSObject.Properties['cwd']) {
        [string]$hookInput.cwd
    }
    else {
        (Get-Location).Path
    }

    $projectRoot = [System.IO.Path]::GetFullPath($projectRoot)
    $claudePath = Join-Path $projectRoot 'CLAUDE.md'
    $instructionsPath = Join-Path $projectRoot 'docs/DEVELOPMENT_INSTRUCTIONS.md'

    foreach ($requiredPath in @($claudePath, $instructionsPath)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            [Console]::Error.WriteLine("Mandatory project instruction file is missing: $requiredPath")
            exit 2
        }
    }

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    $claudeContent = [System.IO.File]::ReadAllText($claudePath, $utf8NoBom)
    $instructionsContent = [System.IO.File]::ReadAllText($instructionsPath, $utf8NoBom)
    if ([string]::IsNullOrWhiteSpace($claudeContent) -or [string]::IsNullOrWhiteSpace($instructionsContent)) {
        [Console]::Error.WriteLine('A mandatory project instruction file is empty.')
        exit 2
    }

    $claudeHash = Get-Sha256 -Value $claudeContent
    $instructionsHash = Get-Sha256 -Value $instructionsContent

    @"
MANDATORY B-UNITED PREFLIGHT
Before planning, editing files, or running tools, apply all rules loaded from CLAUDE.md and its imported docs/DEVELOPMENT_INSTRUCTIONS.md. These files were verified for this prompt.
CLAUDE.md SHA-256: $claudeHash
DEVELOPMENT_INSTRUCTIONS.md SHA-256: $instructionsHash
Do not silently bypass a rule. If the request conflicts with an instruction, stop before making changes, identify the conflict, and request an explicit decision. Do not claim completion until every applicable completion gate has been verified.
"@ | Write-Output
}
catch {
    [Console]::Error.WriteLine("B-United instruction preflight failed: $($_.Exception.Message)")
    exit 2
}

exit 0


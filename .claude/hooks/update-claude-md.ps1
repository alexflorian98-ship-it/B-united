[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Get-PropertyValue {
    param(
        [Parameter(Mandatory = $true)] [object] $Object,
        [Parameter(Mandatory = $true)] [string] $Name,
        [object] $Default = $null
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Default
    }

    return $property.Value
}

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
    $inputText = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($inputText)) {
        exit 0
    }

    $hookInput = $inputText | ConvertFrom-Json
    if ([bool](Get-PropertyValue -Object $hookInput -Name 'stop_hook_active' -Default $false)) {
        exit 0
    }

    $projectRoot = [string](Get-PropertyValue -Object $hookInput -Name 'cwd' -Default (Get-Location).Path)
    $projectRoot = [System.IO.Path]::GetFullPath($projectRoot)
    $claudePath = Join-Path $projectRoot 'CLAUDE.md'
    $statePath = Join-Path $projectRoot '.claude/session-state.json'

    if (-not (Test-Path -LiteralPath $claudePath)) {
        exit 0
    }

    Push-Location $projectRoot
    try {
        & git rev-parse --is-inside-work-tree *> $null
        if ($LASTEXITCODE -ne 0) {
            exit 0
        }

        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'SilentlyContinue'
        try {
            $statusLines = @(& git status --short --untracked-files=all 2>$null) |
                Where-Object {
                    $_ -notmatch '^..\s+CLAUDE\.md$' -and
                    $_ -notmatch '^..\s+\.claude/session-state\.json$'
                }

            $statusText = ($statusLines -join "`n")
            $unstagedDiff = @(& git diff --binary -- . ':(exclude)CLAUDE.md' ':(exclude).claude/session-state.json' 2>$null) -join "`n"
            $stagedDiff = @(& git diff --cached --binary -- . ':(exclude)CLAUDE.md' ':(exclude).claude/session-state.json' 2>$null) -join "`n"
            $untrackedPaths = @(& git ls-files --others --exclude-standard 2>$null) |
                Where-Object {
                    $_ -ne 'CLAUDE.md' -and
                    $_ -ne '.claude/session-state.json'
                }
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }

        $untrackedSignatures = [System.Collections.Generic.List[string]]::new()
        foreach ($relativePath in $untrackedPaths) {
            $absolutePath = Join-Path $projectRoot $relativePath
            if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
                $fileHash = (Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash.ToLowerInvariant()
                $untrackedSignatures.Add("$relativePath`:$fileHash")
            }
        }

        $fingerprintSource = @(
            $statusText
            $unstagedDiff
            $stagedDiff
            ($untrackedSignatures -join "`n")
        ) -join "`n---`n"
        $fingerprint = Get-Sha256 -Value $fingerprintSource
        $sessionId = [string](Get-PropertyValue -Object $hookInput -Name 'session_id' -Default 'unknown')

        $state = $null
        if (Test-Path -LiteralPath $statePath) {
            try {
                $state = Get-Content -Raw -LiteralPath $statePath | ConvertFrom-Json
            }
            catch {
                $state = $null
            }
        }

        $lastFingerprint = if ($null -ne $state) {
            [string](Get-PropertyValue -Object $state -Name 'fingerprint' -Default '')
        }
        else {
            ''
        }

        if ($fingerprint -eq $lastFingerprint) {
            exit 0
        }

        $commit = (& git rev-parse --short HEAD 2>$null | Select-Object -First 1)
        if ([string]::IsNullOrWhiteSpace($commit)) {
            $commit = 'no-commit'
        }

        $summary = [string](Get-PropertyValue -Object $hookInput -Name 'last_assistant_message' -Default '')
        $summary = ($summary -replace '\s+', ' ').Trim()
        if ($summary.Length -gt 240) {
            $summary = $summary.Substring(0, 237) + '...'
        }

        $timestamp = [DateTimeOffset]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss ''UTC''')
        $entryLines = [System.Collections.Generic.List[string]]::new()
        $entryLines.Add("### $timestamp - Session ``$sessionId``")
        $entryLines.Add('')
        $entryLines.Add("- Base commit: ``$commit``")
        if (-not [string]::IsNullOrWhiteSpace($summary)) {
            $entryLines.Add("- Outcome: $summary")
        }

        if ($statusLines.Count -eq 0) {
            $entryLines.Add('- Workspace changes: clean working tree')
        }
        else {
            $entryLines.Add('- Workspace changes:')
            foreach ($line in ($statusLines | Select-Object -First 50)) {
                $safeLine = ([string]$line).Replace('`', '\`')
                $entryLines.Add("  - ``$safeLine``")
            }
            if ($statusLines.Count -gt 50) {
                $entryLines.Add("  - ... and $($statusLines.Count - 50) more paths")
            }
        }

        $content = Get-Content -Raw -LiteralPath $claudePath
        $startMarker = '<!-- SESSION-CHANGES:START -->'
        $endMarker = '<!-- SESSION-CHANGES:END -->'
        $startIndex = $content.IndexOf($startMarker, [StringComparison]::Ordinal)
        $endIndex = $content.IndexOf($endMarker, [StringComparison]::Ordinal)

        if ($startIndex -lt 0 -or $endIndex -le $startIndex) {
            exit 0
        }

        $existingStart = $startIndex + $startMarker.Length
        $existing = $content.Substring($existingStart, $endIndex - $existingStart).Trim()
        if ($existing -eq 'No Claude Code session changes recorded yet.') {
            $existing = ''
        }

        $newEntry = $entryLines -join "`n"
        $combined = if ([string]::IsNullOrWhiteSpace($existing)) {
            $newEntry
        }
        else {
            "$newEntry`n`n$existing"
        }

        $entries = [regex]::Matches($combined, '(?ms)^### .*?(?=^### |\z)') |
            Select-Object -First 20 |
            ForEach-Object { $_.Value.Trim() }
        $trimmedHistory = $entries -join "`n`n"

        $prefix = $content.Substring(0, $existingStart)
        $suffix = $content.Substring($endIndex)
        $updatedContent = "$prefix`n$trimmedHistory`n$suffix"
        [System.IO.File]::WriteAllText($claudePath, $updatedContent, [System.Text.UTF8Encoding]::new($false))

        $statePayload = [ordered]@{
            sessionId = $sessionId
            fingerprint = $fingerprint
            updatedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        } | ConvertTo-Json
        [System.IO.File]::WriteAllText($statePath, $statePayload, [System.Text.UTF8Encoding]::new($false))
    }
    finally {
        Pop-Location
    }
}
catch {
    [Console]::Error.WriteLine("B-United CLAUDE.md update hook failed: $($_.Exception.Message)")
    exit 1
}

exit 0

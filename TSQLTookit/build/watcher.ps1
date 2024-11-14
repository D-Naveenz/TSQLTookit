using namespace System.IO
using module ./antlr.psm1

$rootPath = Split-Path $PSScriptRoot -Parent    # Root path of the project
$javaTarget = 11                                # Minimum Java version required
$antlrVersion = "4.13.2"                        # Antlr4 version required

# Grammar file watcher
$watcher = New-Object FileSystemWatcher
$watcher.Path = Join-Path $rootPath -ChildPath "Grammar"
$watcher.Filter = "*.g4"
$watcher.NotifyFilter = [NotifyFilters]::LastWrite
$watcher.IncludeSubdirectories = $false

# ANTLR builder object
$antlr = [Antlr]::new($rootPath, $antlrVersion, $javaTarget)

# Variable to store the last action timestamp
$lastRunTime = [datetime]::MinValue
$debounceInterval = [timespan]::FromSeconds(5)  # Set debounce interval (e.g., 5 seconds)

# Event handler to run on file change
$action = {
    if ($null -ne $Event.SourceEventArgs.FullPath) {
        $currentTime = Get-Date
        if ($currentTime - $script:lastRunTime -ge $script:debounceInterval) {
            Write-Host "File $($Event.SourceEventArgs.ChangeType): $($Event.SourceEventArgs.FullPath)"
            $antlr.build()
            $script:lastRunTime = $currentTime  # Update last run time
        } else {
            Write-Host "Duplicate change detected. Skipping execution to avoid multiple triggers."
        }
    }
}

# Register events
Register-ObjectEvent $watcher "Changed" -Action $action
Register-ObjectEvent $watcher "Created" -Action $action

# Keep the script running
Write-Host "Watching for changes in .g4 files..."
while ($true) { Start-Sleep -Seconds 3 }


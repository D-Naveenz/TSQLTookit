class Antlr {
    # Properties
    [string] $AntlrVersion
    [string] $JavaVersion
    [string] $AntlrLocation
    [string] $GrammarLocation
    [array]  $Folders = "Lib", "Grammar"

    Antlr([string]$baseDir, [string]$antlrVersion, [int]$javaVersion) {
        $this.AntlrVersion = $antlrVersion
        $this.AntlrLocation = Join-Path -Path $baseDir -ChildPath "Lib\antlr-$antlrVersion-complete.jar"
        $this.GrammarLocation = Join-Path -Path $baseDir -ChildPath "Grammar"

        # Create the directories if they do not exist
        foreach ($folder in $this.Folders) {
            $path = Join-Path -Path $baseDir -ChildPath $folder
            if (-not (Test-Path $path)) {
                New-Item -ItemType Directory -Path $path
            }
        }

        # Check Java is installed and the version is minimum 11
        $isValidJavaVersion = $this.SetJavaVersionIfValid($javaVersion)
        if (-not $isValidJavaVersion) {
            Write-Host "The minimum Java version required is $javaVersion"
            exit 1
        }

        # Check Antlr4 is installed
        $isValidAntlr = $this.IsAntlrAvailable($antlrVersion)
        if (-not $isValidAntlr) {
            Write-Host "Antlr4 is required to build the project"
            exit 1
        }
    }

    [bool] SetJavaVersionIfValid([int]$v) {
        # Capture the version number
        $version = (java -version 2>&1 | Select-String -Pattern '(?<=version ")(.*)(?=")').Matches[0].Groups[1].Value;
    
        # Match the major version number with the target version
        $majorVersion = [int]($version | Select-String -Pattern '^\d+').Matches[0].Value;
        $result = $majorVersion -ge $v
        if ($result) {
            $this.JavaVersion = $version
        }

        return $result
    }

    [bool] IsAntlrAvailable([string]$v) {
        # Check Antlr4 is installed
        $antlrResult = java -jar $this.AntlrLocation 2>&1 | Select-String "ANTLR Parser Generator"
        if ($null -eq $antlrResult) {
            # User prompt to install Antlr4
            Write-Host "Antlr4 library is missing. Do you want to install it now? (Y/N)"
            $response = Read-Host
            if ($response -eq "Y") {
                # Download and install Antlr4
                $url = "https://www.antlr.org/download/antlr-$this.AntlrVersion-complete.jar"
                $output = $this.AntlrLocation
                Invoke-WebRequest -Uri $url -OutFile $output
    
                # Check if the download was successful
                if (Test-Path $output) {
                    Write-Host "Antlr4 library has been installed successfully"
                    return $true
                }
                else {
                    Write-Host "Failed to install Antlr4 library"
                    exit 1
                }
            }
    
            return $false
        }
    
        return $true
    }

    [void] Cleanup() {
        # Remove everything in the Grammar directory except the .g4 files
        $content = Get-ChildItem -Path $this.GrammarLocation
        foreach ($item in $content) {
            if ($item.Extension -ne ".g4") {
                Remove-Item -Path $item.FullName -Recurse -Force
            }
        }
    }

    [void] build() {
        # Get the list of grammar files
        $grammarFiles = Get-ChildItem -Path $this.GrammarLocation -Filter *.g4

        # Generate the parser files
        foreach ($grammarFile in $grammarFiles) {
            $grammarName = $grammarFile.BaseName
            $outputDir = Join-Path -Path $this.GrammarLocation -ChildPath $grammarName
            if (-not (Test-Path $outputDir)) {
                New-Item -ItemType Directory -Path $outputDir
            }

            Write-Host "Generating parser for: $grammarName"
            java -jar $this.AntlrLocation -Dlanguage=CSharp $grammarFile -o $outputDir
        }
    }
}
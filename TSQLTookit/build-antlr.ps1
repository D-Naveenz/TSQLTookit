$javaTarget = 11            # Minimum Java version required
$antlrVersion = "4.13.2"    # Antlr4 version required

$antlrLocation = ".\Lib\antlr-$($antlrVersion)-complete.jar"
$parsersLocation = ".\Parsers"

function GetJavaVersionIfValid {
    param (
        [int]$v
    )
    
    # Capture the version number
    $javaVersion = (java -version 2>&1 | Select-String -Pattern '(?<=version ")(.*)(?=")').Matches[0].Groups[1].Value;

    # Match the major version number with the target version
    $majorVersion = [int]($javaVersion | Select-String -Pattern '^\d+').Matches[0].Value;
    if ($majorVersion -ge $v) {
        return $javaVersion
    } else {
        return $null
    }
}

function IsAntlrAvailable {
    param (
        [string]$v
    )
    
    # Check Antlr4 is installed
    $antlrResult = java -jar $antlrLocation 2>&1 | Select-String "ANTLR Parser Generator"
    if ($null -eq $antlrResult) {
        # Create the Lib directory if it does not exist
        if (-not (Test-Path ".\Lib")) {
            New-Item -ItemType Directory -Path ".\Lib"
        }

        # User prompt to install Antlr4
        Write-Host "Antlr4 library is missing. Do you want to install it now? (Y/N)"
        $response = Read-Host
        if ($response -eq "Y") {
            # Download and install Antlr4
            $url = "https://www.antlr.org/download/antlr-$($antlrVersion)-complete.jar"
            $output = $antlrLocation
            Invoke-WebRequest -Uri $url -OutFile $output

            # Check if the download was successful
            if (Test-Path $output) {
                Write-Host "Antlr4 library has been installed successfully"
                return $true
            } else {
                Write-Host "Failed to install Antlr4 library"
                exit 1
            }
        }

        return $false
    }

    return $true
}

function GetGrammarFiles {
    param (
        [string]$location
    )
    
    if (-not (Test-Path $location)) {
        return $null
    }

    return Get-ChildItem -Path $location -Filter *.g4
}

# Check Java is installed and the version is minimum 11
$javaVersion = GetJavaVersionIfValid -v $javaTarget
if ($null -eq $javaVersion) {
    Write-Host "Java 11 is required to build the project"
    exit 1
} else {
    Write-Host "Java: $($javaVersion)"
}

# Check Antlr4 is installed
if (IsAntlrAvailable -v $antlrVersion) {
    Write-Host "Antlr4: $($antlrVersion)"
} else {
    Write-Host "Antlr4 is required to build the project"
    exit 1
}

# Get the list of grammar files
$grammarFiles = GetGrammarFiles -location $parsersLocation
if ($null -eq $grammarFiles) {
    Write-Host "No grammar files found in the Parsers directory"
    exit 1
} else {
    # Generate the parser files
    foreach ($grammarFile in $grammarFiles) {
        $grammarName = $grammarFile.BaseName
        $outputDir = ".\Generated\$grammarName"
        if (-not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir
        }

        Write-Host "Generating parser for: $grammarName"
        java -jar $antlrLocation -Dlanguage=CSharp $grammarFile -o $outputDir
    }
}